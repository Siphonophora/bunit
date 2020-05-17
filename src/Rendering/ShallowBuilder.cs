using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bunit.Rendering
{
    public static class ShallowBuilder
    {
        public static RenderFragment CreateShallowFragement<TComponent>() where TComponent : class, IComponent
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
                //var AddFrame = builder.GetType().GetMethod("Append", BindingFlags.Instance | BindingFlags.NonPublic);
                // This will be used to track skipped frames
                //var contFrame = RenderTreeFrame
                //builder.OpenComponent<Shallow>(0);
                //builder.OpenComponent(0, typeof(TComponent));
                int skipTo = 0;
                // Check each frame in the RenderTree
                foreach (var frame in fr.Array)
                {
                    Trace.WriteLine($"Got Frame: {frame.Sequence} : {frame.FrameType} : {skipTo}");

                    // GetFrames returns at least 16 frames, filled with FrameType None but the //
                    //   renderer doesn't like them, so skip them or get errors if (frame.FrameType !=
                    //RenderTreeFrameType.None) { // This is how we shallow render - by skipping
                    //       Component frames
                    if (frame.FrameType == RenderTreeFrameType.Component)
                    { // Set the next sequence we are going to allow to render
                        skipTo = frame.Sequence + frame.ComponentSubtreeLength;
                        Trace.WriteLine($"Skipping Component with length: {frame.ComponentSubtreeLength}");

                        // The renderer doesn't like it if we just drop the frame, so for now I am
                        // replacing it with comment text saying the name of the component but you
                        // could do something different
                        Trace.WriteLine("Added Commented Markup");
                        builder.AddMarkupContent(frame.Sequence, $"<{frame.ComponentType.Name} blazor:renderSkipped=\"true\" blazor:renderSkippedFrameCount=\"{frame.ComponentSubtreeLength}\"/>");

                        for (int i = 1; i < frame.ComponentSubtreeLength; i++)
                        {
                            Trace.WriteLine("Added Commented Markup");
                            builder.AddMarkupContent(frame.Sequence + i, string.Empty);
                        }
                    }
                    else
                    {
                        if (frame.Sequence >= skipTo)
                        {
                            // Put the rendered frames we want on the actual RenderTree
                            Trace.WriteLine("Added Frame");
                            AddFrame.Invoke(builder, new object[] { frame });
                        }
                        else
                        {
                            Trace.WriteLine("Skipped Frame");
                        }
                    }
                }
            };
        }
    }
}
