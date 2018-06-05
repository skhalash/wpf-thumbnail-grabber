using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpDX.MediaFoundation;

namespace ThumbnailGrabber
{
    public static class VideoThumbnailGrabber
    {
        static VideoThumbnailGrabber()
        {
            MediaManager.Startup(true);
        }

        public static BitmapSource Grab(byte[] videoFileBytes)
        {
            var sourceReader = ConfigureSourceReader(videoFileBytes);
            var (frameWidth, frameHeight) = GetVideoFrameSize(sourceReader);

            var typeIn = new MediaType();
            typeIn.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
            typeIn.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Rgb32);
            sourceReader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, typeIn);

            Sample sample = null;
            const int samplesToSkip = 5;
            for (var i = 0; i < samplesToSkip; ++i)
            {
                sample = sourceReader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlags.None,
                    out _, out var flags, out _);

                if (flags.HasFlag(SourceReaderFlags.Currentmediatypechanged))
                {
                    (frameWidth, frameHeight) = GetVideoFrameSize(sourceReader);
                }
                else if (flags.HasFlag(SourceReaderFlags.Endofstream))
                {
                    throw new InvalidOperationException();
                }
            }

            var contiguousBuffer = sample.ConvertToContiguousBuffer();

            return CreateBitmapSource(contiguousBuffer, frameWidth, frameHeight);
        }

        private static SourceReader ConfigureSourceReader(byte[] videoFileBytes)
        {
            var attributes = new MediaAttributes();
            attributes.Set(SourceReaderAttributeKeys.EnableVideoProcessing, 1);
            return new SourceReader(videoFileBytes, attributes);
        }

        private static (int frameWidth, int frameHeight) GetVideoFrameSize(SourceReader sourceReader)
        {
            var typeOut = sourceReader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
            var (frameHeight, frameWidth) = Unpack(typeOut.Get(MediaTypeAttributeKeys.FrameSize));
            return (frameWidth, frameHeight);
        }

        private static (int, int) Unpack(long value)
        {
            var first = (int) (value & uint.MaxValue);
            var second = (int) (value >> 32);

            return (first, second);
        }

        private static BitmapSource CreateBitmapSource(MediaBuffer contiguousBuffer, int frameWidth, int frameHeight)
        {
            try
            {
                var thumbnailBuffer = contiguousBuffer.Lock(out _, out var thumbnailBufferSize);
                return BitmapSource.Create(
                    frameWidth,
                    frameHeight,
                    96,
                    96,
                    PixelFormats.Bgr32, null, thumbnailBuffer, thumbnailBufferSize, frameWidth * 4);
            }
            finally
            {
                contiguousBuffer.Unlock();
            }
        }
    }
}