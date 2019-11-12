using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace YoutubeMusicDesktop {
    class Program {
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        const int VK_RETURN = 0x0D;
        const int WM_KEYDOWN = 0x100;

        static async Task Main(string[] args) {
            var cancel = new CancellationTokenSource();
            var token = cancel.Token;

            Console.CancelKeyPress += (o, e) => {
                e.Cancel = true;
                var hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                PostMessage(hWnd, WM_KEYDOWN, VK_RETURN, 0);
                cancel.Cancel();
            };

            await Task.Run(() => Youtube(token));
            Console.WriteLine("exit");
        }

        static async Task Youtube(CancellationToken token) {
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Console.WriteLine("fetch complete");
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions {
                Headless = false,
                DefaultViewport = null,
                Args = new[] {
                    "--window-size=400,800",
                },
            });
            Console.WriteLine("launch complete");
            var page = await browser.NewPageAsync();
            Console.WriteLine("get first page complete");
            var resp = await page.GoToAsync("https://music.youtube.com/watch?v=AMCwYdTJ_PE");
            Console.WriteLine("goto complete");

            var adBtn = await page.QuerySelectorAsync(".ytp-ad-skip-button");
            if (adBtn != null) {
                await adBtn.ClickAsync();
            }

            var playBtn = await page.WaitForSelectorAsync("#play-pause-button");
            var prevBtn = await page.WaitForSelectorAsync(".previous-button");
            var nextBtn = await page.WaitForSelectorAsync(".next-button");
            var title = await page.WaitForSelectorAsync(".title.ytmusic-player-bar");
            var progress = await page.WaitForSelectorAsync("#progress-bar");

            while (true) {
                if (token.IsCancellationRequested) break;
                var read = Console.ReadLine();
                if (read == "ad") {
                    var ad = await page.QuerySelectorAsync(".ytp-ad-skip-button");
                    if (ad != null) {
                        await ad.ClickAsync();
                        Console.WriteLine("skipped");
                    }
                }
                if (read == "reload") {
                    playBtn = await page.WaitForSelectorAsync("#play-pause-button");
                    prevBtn = await page.WaitForSelectorAsync(".previous-button");
                    nextBtn = await page.WaitForSelectorAsync(".next-button");
                    title = await page.WaitForSelectorAsync(".title.ytmusic-player-bar");
                    progress = await page.WaitForSelectorAsync("#progress-bar");
                }
                if (read == "play") {
                    await playBtn.ClickAsync();
                }
                if (read == "prev") {
                    await prevBtn.ClickAsync();
                }
                if (read == "next") {
                    await nextBtn.ClickAsync();
                }

                if (read == "isplaying") {
                    Console.WriteLine(await IsPlaying());
                }
                if (read == "title") {
                    Console.WriteLine(await Title());
                }
                if (read == "len") {
                    var len = await Length();
                    var now = await Current();
                    Console.WriteLine($"{len} / {now}");
                }
            }

            async Task<bool> IsPlaying() {
                var p = await playBtn.GetPropertyAsync("title");
                var text = await p.JsonValueAsync<string>();
                return text == "일시중지";
            }

            async Task<string> Title() {
                var p = await title.GetPropertyAsync("title");
                return await p.JsonValueAsync<string>();
            }

            async Task<int> Length() {
                var p = await progress.GetPropertyAsync("aria-valuemax");
                var text = await p.JsonValueAsync<string>();
                return int.TryParse(text, out var result) ? result : -1;
            }

            async Task<int> Current() {
                var p = await progress.GetPropertyAsync("aria-valuenow");
                var text = await p.JsonValueAsync<string>();
                return int.TryParse(text, out var result) ? result : -1;
            }
        }
    }
}
