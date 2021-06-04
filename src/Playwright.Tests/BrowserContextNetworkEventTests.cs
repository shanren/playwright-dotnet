using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextNetworkEventTests : BrowserTestEx
    {
        [PlaywrightTest("browsercontext-network-event.spec.ts", "BrowserContext.Events.Request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextEventsRequest()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            List<IRequest> requests = new();
            context.Request += (_, req) => requests.Add(req);

            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");

            var page1 = await context.RunAndWaitForPageAsync(() => page.ClickAsync("a"));
            await page1.WaitForLoadStateAsync();

            var urls = requests.Select(x => x.Url).ToArray();
            Assert.Contains(Server.EmptyPage, urls);
            Assert.Contains($"{Server.Prefix}/one-style.html", urls);
            Assert.Contains($"{Server.Prefix}/one-style.css", urls);
        }

        /// <playwright-file>browsercontext-network-event.spec.ts</playwright-file>
        /// <playwright-it>BrowserContext.Events.Response</playwright-it>
        [PlaywrightTest("browsercontext-network-event.spec.ts", "BrowserContext.Events.Response")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextEventsResponse()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            List<IResponse> responses = new();
            context.Response += (_, res) => responses.Add(res);

            await page.GotoAsync(Server.EmptyPage);
            await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");

            var page1 = await context.RunAndWaitForPageAsync(() => page.ClickAsync("a"));
            await page1.WaitForLoadStateAsync();

            var urls = responses.Select(x => x.Url).ToArray();
            Assert.Contains(Server.EmptyPage, urls);
            Assert.Contains($"{Server.Prefix}/one-style.html", urls);
            Assert.Contains($"{Server.Prefix}/one-style.css", urls);
        }

        /// <playwright-file>browsercontext-network-event.spec.ts</playwright-file>
        /// <playwright-it>BrowserContext.Events.RequestFailed</playwright-it>
        [PlaywrightTest("browsercontext-network-event.spec.ts", "BrowserContext.Events.RequestFailed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextEventsRequestfailed()
        {
            Server.SetRoute("/one-style.css", (ctx) =>
            {
                ctx.Response.Headers["Content-Type"] = "text/css";
                ctx.Abort();
                return Task.CompletedTask;
            });

            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            List<IRequest> failedRequests = new();
            context.RequestFailed += (_, failedRequest) => failedRequests.Add(failedRequest);

            await page.GotoAsync($"{Server.Prefix}/one-style.html");
            Assert.AreEqual(1, failedRequests.Count);

            var failedRequest = failedRequests[0];
            Assert.AreEqual($"{Server.Prefix}/one-style.css", failedRequest.Url);

            Assert.IsNull(await failedRequest.ResponseAsync());
            Assert.AreEqual("stylesheet", failedRequest.ResourceType);
            Assert.IsNotNull(failedRequest.Frame);
        }

        /// <playwright-file>browsercontext-network-event.spec.ts</playwright-file>
        /// <playwright-it>BrowserContext.Events.RequestFinished</playwright-it>
        [PlaywrightTest("browsercontext-network-event.spec.ts", "BrowserContext.Events.RequestFinished")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task BrowserContextEventsRequestfinished()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var request = await page.RunAndWaitForRequestFinishedAsync(() => page.GotoAsync(Server.EmptyPage));
            Assert.AreEqual(Server.EmptyPage, request.Url);

            var response = await request.ResponseAsync();
            Assert.NotNull(response);
            Assert.NotNull(request.Frame);
            Assert.AreEqual(Server.EmptyPage, request.Frame.Url);
            Assert.IsNull(request.Failure);
        }

        /// <playwright-file>browsercontext-network-event.spec.ts</playwright-file>
        /// <playwright-it>should fire events in proper order</playwright-it>
        [PlaywrightTest("browsercontext-network-event.spec.ts", "should fire events in proper order")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireEventsInProperOrder()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            List<string> events = new();
            context.Request += (_, _) => events.Add("request");
            context.Response += (_, _) => events.Add("response");
            context.RequestFinished += (_, _) => events.Add("requestFinished");

            await page.RunAndWaitForRequestFinishedAsync(() => page.GotoAsync(Server.EmptyPage));

            CollectionAssert.AreEqual(new string[]
            {
                "request",
                "response",
                "requestFinished"
            }, events.ToArray());
        }
    }
}
