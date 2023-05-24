using ImageResizer.Handlers;
using ImageResizer.Settings;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace ImageResizer.Composers
{
    internal class ImageResizerComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.AddNotificationHandler<MediaSavingNotification, ImageResizerHandler>();
            builder.Services.Configure<ImageResizerSettings>(builder.Config.GetSection("ImageResizer"));
        }
    }
}