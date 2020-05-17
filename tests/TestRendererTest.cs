using System;
using System.IO;
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
            //var sut = RenderComponent<ChildrenHolder>();
            var sut = RenderComponent<ClickCounter>();

            File.WriteAllText($@"c:\temp\click counter markup {DateTime.Now.ToString("yyyyMMdd HHmmss")}.txt", sut.Markup);

            res.RenderCount.ShouldBe(1);

            sut.Find("button").Click();

            res.RenderCount.ShouldBe(2);
        }
    }
}
