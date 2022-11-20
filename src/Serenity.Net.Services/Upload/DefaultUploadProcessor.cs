﻿
using System.IO;

namespace Serenity.Web
{
    public class DefaultUploadProcessor : IUploadProcessor
    {
        private readonly IImageProcessor imageProcessor;
        private readonly IUploadStorage uploadStorage;
        private readonly IUploadValidator uploadValidator;
        private readonly ITextLocalizer localizer;
        private readonly IExceptionLogger logger;

        public DefaultUploadProcessor(IImageProcessor imageProcessor, IUploadStorage uploadStorage, IUploadValidator uploadValidator,
            ITextLocalizer localizer, IExceptionLogger logger = null)
        {
            this.imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
            this.uploadStorage = uploadStorage ?? throw new ArgumentNullException(nameof(uploadStorage));
            this.uploadValidator = uploadValidator ?? throw new ArgumentNullException(nameof(uploadValidator));
            this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            this.logger = logger;
        }

        public ProcessedUploadInfo Process(Stream stream, string filename, IUploadEditor attr)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            if (filename is null)
                throw new ArgumentNullException(nameof(filename));

            if (attr is null)
                throw new ArgumentNullException(nameof(attr));

            var result = new ProcessedUploadInfo
            {
                FileSize = stream.Length
            };

            try
            {
                try
                {
                    uploadValidator.ValidateFile(attr as IUploadFileConstraints ?? new ImageUploadEditorAttribute(),
                        stream, filename, out bool isImageExtension);

                    object image = null;
                    if (isImageExtension)
                        uploadValidator.ValidateImage(attr as IUploadImageContrains ?? new ImageUploadEditorAttribute(),
                            stream, filename, out image);
                    try
                    {
                        uploadStorage.PurgeTemporaryFiles();

                        var basePath = "temporary/" + Guid.NewGuid().ToString("N");
                        stream.Seek(0, SeekOrigin.Begin);
                        result.TemporaryFile = uploadStorage.WriteFile(basePath + Path.GetExtension(filename), 
                            stream, autoRename: false);
                        result.IsImage = isImageExtension && image != null;
                        if (result.IsImage)
                        {
                            var (width, height) = imageProcessor.GetImageSize(image);
                            result.ImageWidth = width;
                            result.ImageHeight = height;
                            stream.Close();
                            result.TemporaryFile = UploadStorageExtensions.ScaleImageAndCreateAllThumbs(image, imageProcessor,
                                attr as IUploadImageOptions ?? new ImageUploadEditorAttribute(),
                                uploadStorage, result.TemporaryFile);
                        }
                        result.Success = true;
                    }
                    finally
                    {
                        (image as IDisposable)?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = ex.Message;
                    result.Success = false;
                    ex.Log(logger);
                }
            }
            finally
            {
                if (!result.Success && !string.IsNullOrEmpty(result.TemporaryFile))
                    uploadStorage.DeleteFile(result.TemporaryFile);

                stream?.Dispose();
            }

            return result;
        }
    }
}
