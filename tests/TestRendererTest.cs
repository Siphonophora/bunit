using System;
using System.Linq;
using Bunit.SampleComponents;
using Shouldly;
using Xunit;

namespace Bunit
{
    public class TestRendererTest : ComponentTestFixture
    {
        [Fact(DisplayName = "Renderer pushes render events to subscribers when renders occur")]
        public void Test001()
        {
            var res = new ConcurrentRenderEventSubscriber(Renderer.RenderEvents);
            var sut = RenderComponent<ChildrenHolder>();

            res.RenderCount.ShouldBe(1);

            sut.Find("button").Click();

            res.RenderCount.ShouldBe(2);
        }
    }
}
