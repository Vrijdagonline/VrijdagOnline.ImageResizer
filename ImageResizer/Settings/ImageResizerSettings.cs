namespace ImageResizer.Settings
{
    public class ImageResizerSettings
    {
        public bool ImageResizeDisabled { get; set; } = false;
        public int ImageResizeWidth { get; set; } = 1920;
        public int ImageResizeHeight { get; set; } = 1080;
        public string ImageResizeSuffix { get; set; } = "_resized";
        public bool ImageResizeKeepOriginal { get; set; } = false;
        public bool ImageResizeUpscale { get; set; } = false;
        public int ImageResizePreviewWidth { get; set; } = 240;
        public int ImageResizePreviewHeight { get; set; } = 136;
        public string ImageResizePreviewSuffix { get; set; } = "_preview";
        public bool ImageResizeKeepPreview { get; set; } = false;
        public bool ImageResizeMaintainRatio { get; set; } = true;
        public bool ImageResizeApplyToExistingImages { get; set; } = false;

    }
}