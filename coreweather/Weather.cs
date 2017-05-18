using System;
using System.Collections.Generic;
using System.Text;
using ImageSharp;
using ImageSharp.Drawing;
using DarkSky.Services;
using System.IO;
using DarkSky.Models;
using System.Threading.Tasks;
using System.Numerics;
using SixLabors.Fonts;

namespace coreweather
{
    public static class WeatherColors
    {
        public static Rgba32 White = new Rgba32(255, 255, 255);
        public static Rgba32 Black = new Rgba32(0, 0, 0);
        public static Rgba32 Blue = new Rgba32(8, 15, 255);
        public static Rgba32 LightBlue = new Rgba32(121, 112, 255);
        public static Rgba32 DarkBlue = new Rgba32(41, 25, 92);
        public static Rgba32 Orange = new Rgba32(194, 108, 2);
        public static Rgba32 Red = new Rgba32(198, 19, 2);
        public static Rgba32 Teal = new Rgba32(47, 65, 120);
        public static Rgba32 TealAlso = new Rgba32(172, 177, 237);
        public static Rgba32 Yellow = new Rgba32(205, 185, 0);
    }

    public class DrawCommand
    {
        public bool IsRelative { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
        public Font Font { get; set; }
        public TextAlignment TextAlign { get; set; }
        public Rgba32 Color { get; set; }
        public Image<Rgba32> Image { get; set; }
    }
    public class WeatherRendererDay
    {
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public string Icon { get; set; }
        public string Summary { get; set; }
        public double? HiTemp { get; set; }
        public double? LoTemp { get; set; }
    }
    public class WeatherRendererInfo
    {
        public string Address { get; set; }
        public string Unit { get; set; }
        public DateTime Date { get; set; }
        public string Timezone { get; set; }
        public double? Temperature { get; set; }
        public double? FeelsLike { get; set; }
        public string Alert { get; set; }
        public List<WeatherRendererDay> Forecast { get; set; } = new List<WeatherRendererDay>();
    }

    class Weather
    {
        private string darkSkyApiKey;
        private FontCollection collection;
        private Font mdFont;
        private Font lgFont;
        private Font smFont;
        private Dictionary<string, Image<Rgba32>> images;

        public Weather(string darkSkyApiKey)
        {
            this.darkSkyApiKey = darkSkyApiKey;

            collection = new FontCollection();
            smFont = new Font(collection.Install("Resources/Weather/Star4000 Small.ttf"), 36);
            mdFont = new Font(collection.Install("Resources/Weather/Star4000.ttf"), 36);
            lgFont = new Font(collection.Install("Resources/Weather/Star4000 Large.ttf"), 32);

            images = new Dictionary<string, Image<Rgba32>>();
            foreach (var gif in Directory.GetFiles("Resources/Weather/", "*.gif"))
            {
                var basename = Path.GetFileNameWithoutExtension(gif);
                images.Add(basename, Image.Load(gif));
            }
        }

        // only get the data here, buddy
        public async Task<WeatherRendererInfo> GetForecastAsync(double lat, double lng)
        {
            var WeatherService = new DarkSkyService(darkSkyApiKey);
            DarkSkyResponse forecast = await WeatherService.GetForecast(lat, lng, new DarkSkyService.OptionalParameters
            {
                DataBlocksToExclude = new List<string> { "minutely", "hourly", },
                MeasurementUnits = "auto"
            });

            var info = new WeatherRendererInfo();
            info.Address = "FIXME: geocode addy";
            info.Unit = forecast.Response.Flags.Units == "us" ? "F" : "C";
            info.Date = DateTime.UtcNow;
            info.Timezone = forecast.Response.Timezone;
            info.Temperature = forecast.Response.Currently.Temperature;
            info.FeelsLike = forecast.Response.Currently.ApparentTemperature;
            info.Alert = forecast.Response.Alerts != null ? forecast.Response.Alerts[0].Title : null;

            forecast.Response.Daily.Data.GetRange(0, 4).ForEach(delegate (DataPoint day)
            {
                var dayRender = new WeatherRendererDay()
                {
                    HiTemp = day.TemperatureMax,
                    LoTemp = day.TemperatureMin,
                    Summary = day.Icon, // FIXME: needs a nicer description
                    Icon = day.Icon,
                    Date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(day.Time).ToLocalTime()
                };

                info.Forecast.Add(dayRender);
            });

            return info;
        }

        public MemoryStream RenderWeatherImage(WeatherRendererInfo info)
        {
            MemoryStream output = new MemoryStream();

            using (Image<Rgba32> image = new Image<Rgba32>(images["background"]))
            {
                var now = info.Date; // FIXME: need to add timezone offset, gotta parse IANA string + TimeSpan.FromHours(info.Date.offset);

                var topDateLine = now.ToString("h:mm:ss tt").ToUpper();
                var bottomDateLine = now.ToString("ddd MMM d").ToUpper();
                // FIXME: if alert, need red bottom instead of blue
                var tickerLine = info.Alert != null ? info.Alert + "\n" : "";
                tickerLine += $"Temp: {(int)info.Temperature}°{info.Unit}   Feels Like: {(int)info.FeelsLike}°{info.Unit}";

                // everything except the forecast
                var cmds = new List<DrawCommand> {
                    new DrawCommand() {Text = info.Address, Font = mdFont, X = 150, Y = -5, Color = WeatherColors.White },
                    new DrawCommand() {Text = "Extended Forecast", Font = mdFont, IsRelative = true, X = 0, Y = 36, Color = WeatherColors.Yellow },
                    new DrawCommand() {Text = topDateLine, Font = smFont, X = 695, Y = 1, Color = WeatherColors.White },
                    new DrawCommand() {Text = bottomDateLine, Font = smFont, IsRelative = true, X = 0, Y = 25, Color = WeatherColors.White },
                    new DrawCommand() {Text = tickerLine, Font = mdFont, X = 5, Y = 475, Color = WeatherColors.White },
                };

                int bx = 15, by = 90;
                foreach (var day in info.Forecast)
                {
                    cmds.AddRange(new List<DrawCommand>
                    {
                        new DrawCommand() { X = bx, Y = by },

                        // day of week, icon, summary
                        new DrawCommand() {Text = day.Date.ToString("ddd").ToUpper(), TextAlign = TextAlignment.Center, IsRelative = true, X = 100, Font = mdFont, Color = WeatherColors.Yellow},
                        new DrawCommand() {Image = images[day.Icon], IsRelative = true, X = -90, Y = 50 }, // FIXME: need default
                        new DrawCommand() {Text = day.Summary, TextAlign = TextAlignment.Center, IsRelative = true, X = 90, Y = 125, Font = mdFont, Color = WeatherColors.White},

                        // low temperature
                        new DrawCommand() {Text = "Lo", TextAlign = TextAlignment.Center, IsRelative = true, X = -50, Y = 100, Font = mdFont, Color = WeatherColors.TealAlso},
                        new DrawCommand() {Text = day.LoTemp != null ? Math.Round(day.LoTemp.Value, 0).ToString() : "", TextAlign = TextAlignment.Center, IsRelative = true, X = 0, Y = 45, Font = lgFont, Color = WeatherColors.White},

                        // high temperature
                        new DrawCommand() {Text = "Hi", TextAlign = TextAlignment.Center, IsRelative = true, X = 100, Y = -45, Font = mdFont, Color = WeatherColors.Yellow},
                        new DrawCommand() {Text = day.HiTemp != null ? Math.Round(day.HiTemp.Value, 0).ToString() : "", TextAlign = TextAlignment.Center, IsRelative = true, X = 0, Y = 45, Font = lgFont, Color = WeatherColors.White},
                    });

                    bx += 244;
                }

                int x = 0, y = 0;
                foreach (var cmd in cmds)
                {
                    // reset the position if it's not relative
                    if (cmd.IsRelative)
                    {
                        x += cmd.X;
                        y += cmd.Y;
                    } else
                    {
                        x = cmd.X;
                        y = cmd.Y;
                    }

                    // if we have a text object, use textalignment, color, and text fields
                    if (cmd.Text != null)
                    {
                        var textOpts = new TextGraphicsOptions(false) { TextAlignment = cmd.TextAlign };
                        image.DrawText(cmd.Text, cmd.Font, WeatherColors.Black, new Vector2(x + 2, y + 2), textOpts);
                        image.DrawText(cmd.Text, cmd.Font, cmd.Color, new Vector2(x, y), textOpts);
                    }

                    // if we have an image object, use the image field
                    if (cmd.Image != null)
                    {
                        image.DrawImage(cmd.Image, ImageSharp.PixelFormats.PixelBlenderMode.Normal, 1.0f, new ImageSharp.Size(cmd.Image.Width, cmd.Image.Height), new Point(x, y));
                    }
                }

                image.SaveAsGif(output);
                //image.SaveAsPng(output);
            }

            return output;
        }
    }
}
