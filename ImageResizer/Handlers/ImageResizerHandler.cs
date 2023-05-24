using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;
using Umbraco.Cms.Core.PropertyEditors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Size = SixLabors.ImageSharp.Size;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using ImageResizer.Settings;

namespace ImageResizer.Handlers
{
    public class ImageResizerHandler : INotificationHandler<MediaSavingNotification>
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IOptions<ImageResizerSettings> _resizerSettings;
        private readonly IMediaService _mediaService;
        private readonly MediaUrlGeneratorCollection _mediaUrlGeneratorCollection;

        public ImageResizerHandler(IWebHostEnvironment hostingEnvironment,
            IOptions<ImageResizerSettings> resizerSettings,
            IMediaService mediaService,
            MediaUrlGeneratorCollection mediaUrlGeneratorCollection)
        {
            //Dependency Injection
            _hostingEnvironment = hostingEnvironment;
            _resizerSettings = resizerSettings;
            _mediaService = mediaService;
            _mediaUrlGeneratorCollection = mediaUrlGeneratorCollection;
        }

        /// <summary>
        /// This is called when media items are being saved. It loads the settings from appsettings.json.
        /// Depending on the settings, it create a resized version of the images and optionally replace the original files.
        /// </summary>
        /// <param name="notification">Notification</param>
        public void Handle(MediaSavingNotification notification)
        {
            bool imageResizeDisabled = _resizerSettings.Value.ImageResizeDisabled;

            int imageResizeWidth = _resizerSettings.Value.ImageResizeWidth;
            int imageResizeHeight = _resizerSettings.Value.ImageResizeHeight;
            string imageResizeSuffix = _resizerSettings.Value.ImageResizeSuffix;
            bool imageResizeKeepOriginal = _resizerSettings.Value.ImageResizeKeepOriginal;

            int imageResizePreviewWidth = _resizerSettings.Value.ImageResizePreviewWidth;
            int imageResizePreviewHeight = _resizerSettings.Value.ImageResizePreviewHeight;
            string imageResizePreviewSuffix = _resizerSettings.Value.ImageResizePreviewSuffix;
            bool imageResizeKeepPreview = _resizerSettings.Value.ImageResizeKeepPreview;

            bool imageResizeUpscale = _resizerSettings.Value.ImageResizeUpscale;
            bool imageResizeMaintainRatio = _resizerSettings.Value.ImageResizeMaintainRatio;
            bool imageResizeApplyToExistingImages = _resizerSettings.Value.ImageResizeApplyToExistingImages;

            if (!imageResizeDisabled)
            {
                foreach (IMedia mediaItem in notification.SavedEntities)
                {
                    int resizeWidth = imageResizeWidth;
                    int resizeHeight = imageResizeHeight;
                    string fileNameSuffix = imageResizeSuffix ?? "";

                    int previewWidth = imageResizePreviewWidth;
                    int previewHeight = imageResizePreviewHeight;
                    string previewFileNameSuffix = imageResizePreviewSuffix ?? "";

                    if (!string.IsNullOrEmpty(mediaItem.ContentType.Alias) && mediaItem.ContentType.Alias.ToLower() == "image" && resizeWidth > 0 && resizeHeight > 0)
                    {
                        bool isNew = mediaItem.Id <= 0;
                        if (isNew || imageResizeApplyToExistingImages)
                        {
                            string? serverFilePath = GetServerFilePath(mediaItem);
                            if (serverFilePath != null)
                            {
                                double currentWidth = int.Parse(mediaItem.GetValue<string>("umbracoWidth"));
                                double currentHeight = int.Parse(mediaItem.GetValue<string>("umbracoHeight"));
                                Tuple<int, int> imageSize = GetCorrectWidthAndHeight(resizeWidth, resizeHeight, imageResizeMaintainRatio, currentWidth, currentHeight);
                                bool isDesiredSize = currentWidth == imageSize.Item1 && currentHeight == imageSize.Item2;
                                bool isLargeEnough = currentWidth >= imageSize.Item1 && currentHeight >= imageSize.Item2;

                                if (!isDesiredSize && (isLargeEnough || imageResizeUpscale))
                                {
                                    if (CreateCroppedVersionOfTheFile(imageSize.Item1, imageSize.Item2, fileNameSuffix, imageResizeKeepOriginal, serverFilePath))
                                    {
                                        mediaItem.SetValue("umbracoWidth", imageSize.Item1);
                                        mediaItem.SetValue("umbracoHeight", imageSize.Item2);

                                        _mediaService.Save(mediaItem);
                                    }
                                }

                                if (previewWidth > 0 && previewHeight > 0 && !string.IsNullOrWhiteSpace(previewFileNameSuffix) && imageResizeKeepPreview)
                                {
                                    CreateCroppedVersionOfTheFile(previewWidth, previewHeight,
                                        previewFileNameSuffix, true, serverFilePath);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the correct width and height depending on the desired size and whether the ratio is to be maintained or not.
        /// </summary>
        /// <param name="width">Desired width</param>
        /// <param name="height">Desired height</param>
        /// <param name="maintainRatio">Maintain ratio or not</param>
        /// <param name="currentWidth">Current width of the image</param>
        /// <param name="currentHeight">Current height of the image</param>
        /// <returns>The correct width and height for the new file</returns>
        private static Tuple<int, int> GetCorrectWidthAndHeight(int width, int height, bool maintainRatio, double currentWidth, double currentHeight)
        {
            int newWidth = width;
            int newHeight = height;
            if (maintainRatio)
            {
                double widthToHeightRatio = currentWidth / currentHeight;
                bool isSquare = widthToHeightRatio == 1;
                bool isWider = widthToHeightRatio > 1;
                if (!isSquare && maintainRatio)
                {
                    if (isWider)
                    {
                        newWidth = (int)(newHeight * widthToHeightRatio);
                    }
                    else
                    {
                        newHeight = (int)(newWidth / widthToHeightRatio);
                    }

                }
            }
            return new Tuple<int, int>(newWidth, newHeight);
        }

        /// <summary>
        /// Gets the path of the file on the server
        /// </summary>
        /// <param name="mediaItem">The item to get the path from</param>
        /// <returns>The path of the file on the server</returns>
        private string? GetServerFilePath(IMedia mediaItem)
        {
            var filePath = mediaItem.GetUrl("umbracoFile", _mediaUrlGeneratorCollection);
            if (filePath != null)
            {
                if (!filePath.StartsWith("/media/"))
                {
                    filePath = GetFilePathFromJson(filePath);
                }

                string webRootPath = _hostingEnvironment.WebRootPath.Replace("\\", "/");

                return webRootPath + filePath;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the path of the existing file
        /// </summary>
        /// <param name="filePath">The json version of the file path</param>
        /// <returns>A string for the path of the file</returns>
        private static string? GetFilePathFromJson(string filePath)
        {
            //var jsonFileDetails = JObject.Parse(filePath);
            //string src = jsonFileDetails["src"].ToString();
            //filePath = src;
            return filePath;
        }

        /// <summary>
        /// Creates a cropped version of the file using the given settings
        /// </summary>
        /// <param name="width">Desired width</param>
        /// <param name="height">Desired height</param>
        /// <param name="fileNameSuffix">Suffix of this cropped file</param>
        /// <param name="keepOriginal">Keep the original file or not, if yes, it will rename the new file to the original path after the original is deleted</param>
        /// <param name="originalFilePath">Original file path</param>
        /// <returns></returns>
        private static bool CreateCroppedVersionOfTheFile(int width, int height, string fileNameSuffix, bool keepOriginal, string originalFilePath)
        {
            bool success = false;
            string newFilePath = GetNewFilePath(originalFilePath, fileNameSuffix);
            if (CreateCroppedImage(originalFilePath, newFilePath, width, height))
            {
                if (!keepOriginal)
                {
                    if (DeleteFile(originalFilePath))
                    {
                        success = RenameFile(newFilePath, originalFilePath);
                    }
                    else
                    {
                        success = true;
                    }
                }
                else
                {
                    success = true;
                }
            }
            return success;
        }

        /// <summary>
        /// Creates a cropped version of the image at the size specified in the parameters
        /// </summary>
        /// <param name="originalFilePath">The full path of the original file</param>
        /// <param name="newFilePath">The full path of the new file</param>
        /// <param name="width">The new image width</param>
        /// <param name="height">The new image height</param>
        /// <returns>A bool to show if the method was successful or not</returns>
        private static bool CreateCroppedImage(string originalFilePath, string newFilePath, int width, int height)
        {
            bool success = false;
            try
            {
                using Image image = Image.Load(originalFilePath);
                image.Mutate(x =>
                {
                    x.Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center
                    });
                    x.AutoOrient();
                });

                image.Save(newFilePath);
                success = true;
            }
            catch (Exception)
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Creates a new file path using the original one and adding a suffix to the file name
        /// </summary>
        /// <param name="filePath">The full path of the original file</param>
        /// <param name="fileNameSuffix">The suffix to be used at the end of the file name in the new file path</param>
        /// <param name="folderPath">An out variable to return the folder path</param></para>
        /// <returns>The new file path</returns>
        public static string GetNewFilePath(string filePath, string fileNameSuffix)
        {
            FileInfo fileInfo = new(filePath);
            string? folderPath = fileInfo.DirectoryName;
            string fileExtension = fileInfo.Extension;
            string fullFileName = fileInfo.Name;
            string fileNameWithoutExtension = fullFileName.Substring(0, fullFileName.Length - fileExtension.Length);
            return string.Format("{0}\\{1}{2}{3}", folderPath, fileNameWithoutExtension, fileNameSuffix, fileExtension);
        }

        /// <summary>
        /// Deletes a file, if it exists
        /// </summary>
        /// <param name="filePath">The full path of the file to delete</param>
        /// <returns>A bool to show if the method was successful or not</returns>
        public static bool DeleteFile(string filePath)
        {
            bool success = false;
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                success = true;
            }
            catch (Exception)
            {
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Renames a file by using the Move method.
        /// </summary>
        /// <param name="sourceFileName">The full path of the source file</param>
        /// <param name="destFileName">The full path of the destination file</param>
        /// <returns>A bool to show if the method was successful or not</returns>
        public static bool RenameFile(string sourceFileName, string destFileName)
        {
            bool success = false;
            try
            {
                if (System.IO.File.Exists(sourceFileName) && !System.IO.File.Exists(destFileName))
                {
                    System.IO.File.Move(sourceFileName, destFileName);
                    success = true;
                }
            }
            catch (Exception)
            {
                success = false;
            }
            return success;
        }

        public static void Terminate()
        {
            return;
        }
    }
}