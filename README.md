
# VrijdagOnline.ImageResizer

This project is based on [@prjseal's SmallerImages](https://www.github.com/prjseal/SmallerImages) but rewritten for Umbraco 11+. 

Reduce image size that is being uploaded to the back-office by setting a maximum width and height with the idea to save disk space and improve page speed.

[![NuGet](https://img.shields.io/nuget/dt/VrijdagOnline.ImageResizer?label=Downloads&color=green)](https://www.nuget.org/packages/VrijdagOnline.ImageResizer/)

# Installation
Simply add the package by using donet add package to install the latest version:
```
dotnet add package VrijdagOnline.ImageResizer
```

# Settings
You can change these default settings by adding the section to the appsettings.json file and overwrite the values.

```
"ImageResizer": {
    "ImageResizeDisabled": false,
    "ImageResizeWidth": 1920,
    "ImageResizeHeight": 1080,
    "ImageResizeSuffix": "_resized",
    "ImageResizeKeepOriginal": false,
    "ImageResizeUpscale": false,
    "ImageResizePreviewWidth": 240,
    "ImageResizePreviewHeight": 136,
    "ImageResizePreviewSuffix": "_preview",
    "ImageResizeKeepPreview": false,
    "ImageResizeMaintainRatio": true,
    "ImageResizeApplyToExistingImages": false
}
```

# FAQ
### Does is work with original images?
Change the ImageResizeApplyToExistingImages value to true in appsettings.json
```
"ImageResizeApplyToExistingImages": true,
```
### I want to keep the original image
Change the ImageResizeKeepOriginal value to true in appsettings.json
```
"ImageResizeKeepOriginal": true,
```
### I want to turn on image crop preview
Change the ImageResizeKeepPreview value to true in appsettings.json
```
"ImageResizeKeepPreview": true,
```
# Changelog
### 1.0.3
- Added the Umbraco Marketplace tag
- Added umbraco-marketplace.json config
- Added a changelog section to the README
### 1.0.2
- Fixed an issue where the notification handler kept looping while cropping the image if ImageResizeKeepOriginal was turned on
### 1.0.1
- Changed Umbraco version to 11+
### 1.0.0
- Initial package

# Credits
To [@prjseal](https://www.github.com/prjsea) who originally created the repo.
