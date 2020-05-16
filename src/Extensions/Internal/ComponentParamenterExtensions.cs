using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Bunit.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bunit
{
    /// <summary>
    /// Helpful extensions for working with <see cref="ComponentParameter"/> and collections of these.
    /// </summary>
    internal static class ComponentParamenterExtensions
    {
        private static readonly Type CascadingValueType = typeof(CascadingValue<>);

        /// <summary>
        /// Creates a <see cref="RenderFragment"/> that will render a component of <typeparamref
        /// name="TComponent"/> type, with the provided <paramref name="parameters"/>. If one or
        /// more of the <paramref name="parameters"/> include a cascading values, the <typeparamref
        /// name="TComponent"/> will be wrapped in <see
        /// cref="Microsoft.AspNetCore.Components.CascadingValue{TValue}"/> components.
        /// </summary>
        /// <typeparam name="TComponent">Type of component to render in the render fragment</typeparam>
        /// <param name="parameters">Parameters to pass to the component</param>
        /// <returns>The <see cref="RenderFragment"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>")]
        public static RenderFragment ToComponentRenderFragment<TComponent>(this IReadOnlyList<ComponentParameter> parameters) where TComponent : class, IComponent
        {
            var cascadingParams = new Queue<ComponentParameter>(parameters.Where(x => x.IsCascadingValue));

            if (cascadingParams.Count > 0)
                return CreateCascadingValueRenderFragment(cascadingParams, parameters);
            else
                return CreateComponentRenderFragment(parameters);

            static RenderFragment CreateCascadingValueRenderFragment(Queue<ComponentParameter> cascadingParams, IReadOnlyList<ComponentParameter> parameters)
            {
                var cp = cascadingParams.Dequeue();
                var cascadingValueType = GetCascadingValueType(cp);
                return builder =>
                {
                    builder.OpenComponent(0, cascadingValueType);
                    if (cp.Name is { })
                        builder.AddAttribute(1, nameof(CascadingValue<object>.Name), cp.Name);

                    builder.AddAttribute(2, nameof(CascadingValue<object>.Value), cp.Value);
                    builder.AddAttribute(3, nameof(CascadingValue<object>.IsFixed), true);

                    if (cascadingParams.Count > 0)
                        builder.AddAttribute(4, nameof(CascadingValue<object>.ChildContent), CreateCascadingValueRenderFragment(cascadingParams, parameters));
                    else
                        builder.AddAttribute(4, nameof(CascadingValue<object>.ChildContent), CreateComponentRenderFragment(parameters));

                    builder.CloseComponent();
                };
            }

            static RenderFragment CreateComponentRenderFragment(IReadOnlyList<ComponentParameter> parameters)
            {
                var dummy = new RenderTreeBuilder();

                RenderFragment frag = dummy =>
                {
                    dummy.OpenComponent<TComponent>(1);
                    dummy.CloseComponent();
                    //dummy.OpenComponent<Shallow>(2);
                    //dummy.CloseComponent();
                };

                var serviceProvider = new ServiceCollection().BuildServiceProvider();
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? new NullLoggerFactory();
                var tr = new TestRenderer(serviceProvider, loggerFactory);
                var container = new ContainerComponent(tr);
                tr.AttachTestRootComponent(container);
                container.Render(frag);

                var fr = tr.GetCurrentRenderTreeFrames(2);

                //var fr = dummy.GetFrames().Array;
                var a = 1;

                return builder =>
                {
                    var AddFrame = builder.GetType().GetMethod("Append", BindingFlags.Instance | BindingFlags.NonPublic);
                    // This will be used to track skipped frames
                    int skipTo = 0;
                    builder.OpenComponent<Shallow>(0);
                    // Check each frame in the RenderTree
                    foreach (var frame in fr.Array)
                    {
                        Debug.WriteLine($"Got Frame: {frame.Sequence} : {frame.FrameType} : {skipTo}");

                        // GetFrames returns at least 16 frames, filled with FrameType None but the
                        // renderer doesn't like them, so skip them or get errors
                        if (frame.FrameType != RenderTreeFrameType.None)
                        {
                            // This is how we shallow render - by skipping Component frames
                            if (frame.FrameType == RenderTreeFrameType.Component)
                            {
                                // Set the next sequence we are going to allow to render
                                skipTo = frame.Sequence + frame.ComponentSubtreeLength;

                                // debug - cos you wanna see it happen
                                Debug.WriteLine($"Skipping Component with length: {frame.ComponentSubtreeLength}");

                                // The renderer doesn't like it if we just drop the frame, so for
                                // now I am replacing it with comment text saying the name of the
                                // component but you could do something different
                                for (int i = 0; i < frame.ComponentSubtreeLength; i++)
                                {
                                    builder.AddMarkupContent(frame.Sequence + i, $"<!-- {frame.ComponentType.Name}-{i} -->");
                                }
                            }
                            else
                            {
                                if (frame.Sequence >= skipTo)
                                {
                                    // Put the rendered frames we want on the actual RenderTree
                                    AddFrame.Invoke(builder, new object[] { frame });
                                }
                            }
                        }
                    }
                    builder.CloseComponent();
                };

                //mjc make a builder
                //return builder =>
                //{
                //    builder.OpenComponent(0, typeof(TComponent));

                // foreach (var parameterValue in parameters.Where(x => !x.IsCascadingValue))
                // builder.AddAttribute(1, parameterValue.Name, parameterValue.Value);

                //    builder.CloseComponent();
                //};
            }
        }

        private static Type GetCascadingValueType(ComponentParameter parameter)
        {
            if (parameter.Value is null) throw new InvalidOperationException("Cannot get the type of a null object");
            var cascadingValueType = parameter.Value.GetType();
            return CascadingValueType.MakeGenericType(cascadingValueType);
        }
    }
}
