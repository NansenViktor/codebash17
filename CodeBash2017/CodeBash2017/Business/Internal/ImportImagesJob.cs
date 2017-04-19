using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Web;
using CodeBash2017.Models.Media;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Framework.Configuration;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Hosting;

namespace CodeBash2017.Business.Internal
{

    [ScheduledPlugIn(GUID = "36D1CCCC-5FCE-4481-B0FE-5A1A706F91C3", DisplayName = "Import images", DefaultEnabled = true)]
    [ServiceConfiguration(typeof(ImportImagesJob))]
    public class ImportImagesJob : ScheduledJobBase
    {
        public const string ImageFolder = "Animals";

        private bool _stopSignaled;
        private ContentReference _mediaRoot;
        private ContentReference _pageReference;

        private readonly IBlobFactory _blobFactory;
        private readonly IContentRepository _contentRepository;
        private readonly IConfigurationSource _configurationSource;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly SiteDefinition _siteDefinition;
        private readonly IUrlSegmentGenerator _segmentGenerator;
        private readonly ContentMediaResolver _mediaResolver;

        private readonly CognitiveService _cognitiveService;

        private static ILogger _log = LogManager.GetLogger(typeof(ImportImagesJob));
        private readonly IContentTypeRepository _contentTypeRepository;

        public ImportImagesJob(IContentRepository contentRepository, IBlobFactory blobFactory, IHostingEnvironment hostingEnvironment, 
            SiteDefinition siteDefinition, IUrlSegmentGenerator segmentGenerator, ContentMediaResolver mediaResolver, IContentTypeRepository contentTypeRepository)
        {
            _blobFactory = blobFactory;
            _contentRepository = contentRepository;
            _configurationSource = ConfigurationSource.Instance;
            _hostingEnvironment = hostingEnvironment;
            _siteDefinition = siteDefinition;
            _segmentGenerator = segmentGenerator;
            _mediaResolver = mediaResolver;
            _contentTypeRepository = contentTypeRepository;
            IsStoppable = true;

            _cognitiveService = new CognitiveService();
        }

        public override void Stop()
        {
            _stopSignaled = true;
            base.Stop();
        }

        public override string Execute()
        {
            DeleteAllChildPages();

            return CreateChildPages();
        }

        private void DeleteAllChildPages()
        {
            _contentRepository.DeleteChildren(MediaRoot, true, AccessLevel.NoAccess);
        }

        private string CreateChildPages()
        {
            var zipPath = _configurationSource.GetSetting("codebash:imagezip");
            if (string.IsNullOrEmpty(zipPath))
                return "appsetting 'codebash:imagezip' must be set to a zip with images";

            if (VirtualPathUtility.IsAppRelative(zipPath))
                zipPath = _hostingEnvironment.MapPath(zipPath);

            if (!File.Exists(zipPath))
                return $"Configured zipFile '{zipPath}' does not exist";

            int importedFiles = 0;
            int failedFiles = 0;
            int processedFiles = 0;
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    processedFiles++;

                    if (_stopSignaled)
                        break;
                    try
                    {
                        var urlSegment = _segmentGenerator.Create(entry.Name);
                        var folder = _contentRepository.GetBySegment(MediaRoot, urlSegment, CultureInfo.InvariantCulture);
                        if (folder == null)
                        {
                            var extension = VirtualPathUtilityEx.GetExtension(entry.Name);
                            var mediaType = _mediaResolver.GetFirstMatching(extension);
                            if (mediaType == null)
                            {
                                _log.Warning($"There is no mediatype registered that handle file of type '{extension}'");
                                failedFiles++;
                                continue;
                            }
                            var contentType = _contentTypeRepository.Load(mediaType);
                            var media = _contentRepository.GetDefault<AnimalImage>(MediaRoot, contentType.ID);
                            media.Name = entry.Name;

                            using (var readStream = entry.Open())
                            {
                                // Read the image and save the image
                                media.BinaryData = _blobFactory.CreateBlob(media.BinaryDataContainer, extension);
                                using (var writeStream = media.BinaryData.OpenWrite())
                                {
                                    readStream.CopyTo(writeStream);
                                }                                

                                // Analyze image with MS Cognitive Services
                                var imageMetaData = _cognitiveService.AnalyzeImage(media);
                                media.Name = entry.Name;
                                SetMediaData(imageMetaData, media);

                                // Don't save if there's an error
                                if (media.ErrorCode != null)
                                    throw new EPiServerException($"MS Cognitive Services error code {media.ErrorCode}. Message: {media.ErrorMessage}");
                            }

                            _contentRepository.Save(media, SaveAction.Publish, AccessLevel.NoAccess);

                            importedFiles++;
                            OnStatusChanged($"Imported '{importedFiles}' files");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnStatusChanged($"Failed import on '{entry.Name}': {ex.Message}");
                        _log.Error($"Failed to process file '{entry.Name}'", ex);
                        failedFiles++;
                    }

                    // Throttle the requests to MS Cognitive Services 
                    Thread.Sleep(3000);
                }
            }

            return $"Processed '{processedFiles}' files and imported '{importedFiles}' new files, failed to import '{failedFiles}'. " + (failedFiles > 0 ? "See log for detailed error" : "");
        }

        private void SetMediaData(BingComputerVisionResponse imageMetaData, AnimalImage media)
        {
            if (imageMetaData.StatusCode != null)
            {
                media.ErrorMessage = imageMetaData.Message;
                media.ErrorCode = imageMetaData.StatusCode;
//                _log.Error($"Error analyzing image {media.Name}. Error code {media.ErrorCode} and message: {media.ErrorMessage}");
                return;
            }

            media.Width = imageMetaData.Metadata.Width;
            media.Height = imageMetaData.Metadata.Height;
            media.Format = imageMetaData.Metadata.Format;

            media.Categories = imageMetaData.Categories?.Select(category => new Models.Media.Category
                {
                    Name = category.Name,
                    Score = category.Score,
                    Celebrities = category.Detail?.Celebrities.Select(celebrity => new Models.Media.Celebrity
                        {
                            Name = celebrity.Name,
                            Confidence = celebrity.Confidence,
                            FaceRectangle = new Models.Media.FaceRectangleObject
                            {
                                Height = celebrity.FaceRectangle?.Height ?? 0,
                                Left = celebrity.FaceRectangle?.Left ?? 0,
                                Top = celebrity.FaceRectangle?.Top ?? 0,
                                Width = celebrity.FaceRectangle?.Width ?? 0,
                            }
                        })
                        .ToList()
                })
                .ToList();

            if (imageMetaData.Adult != null)
            {
                media.AdultScore = imageMetaData.Adult.AdultScore;
                media.IsAdultContent = imageMetaData.Adult.IsAdultContent;
                media.IsRacyContent = imageMetaData.Adult.IsRacyContent;
                media.RacyScore = imageMetaData.Adult.RacyScore;
            }

            media.AccentColor = HexEncode(imageMetaData.Color.AccentColor);
            media.DominantColorBackground = HexEncode(imageMetaData.Color.DominantColorBackground);
            media.DominantColorForeground = HexEncode(imageMetaData.Color.DominantColorForeground);
            media.DominantColors = imageMetaData.Color.DominantColors.Select(HexEncode).ToArray();
            media.IsBWImg = imageMetaData.Color.IsBWImg;

            media.Tags = imageMetaData.Tags?.Select(t => new Models.Media.Tag
                {
                    Name = t.Name,
                    Confidence = t.Confidence
                })
                .ToList();

            if (imageMetaData.Description != null)
            {
                media.DescriptionTags = imageMetaData.Description.Tags;
                media.DescriptionCaptions = imageMetaData.Description.Captions?.Select(
                        t => new Models.Media.Caption
                        {
                            Confidence = t.Confidence,
                            Text = t.Text
                        })
                    .ToList();
            }

            if (imageMetaData.Faces != null)
            {
                media.Faces = imageMetaData.Faces.Select(f => new Face
                    {
                        FaceRectangle = new FaceRectangleObject
                        {
                            Width = f.FaceRectangle?.Width ?? 0,
                            Top = f.FaceRectangle?.Top ?? 0,
                            Height = f.FaceRectangle?.Height ?? 0,
                            Left = f.FaceRectangle?.Left ?? 0,
                        }
                    })
                    .ToList();
            }

            if (imageMetaData.ImageType != null)
            {
                media.ClipArtType = imageMetaData.ImageType.ClipArtType;
                media.LineDrawingType = imageMetaData.ImageType.LineDrawingType;
            }
        }

        private string HexEncode(string color)
        {
            int result;
            if (Int32.TryParse(color, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result))
                return "#" + color;

            return color;
        }

        private ContentReference MediaRoot
        {
            get
            {
                if (_mediaRoot == null)
                {
                    var mediaFolder = _contentRepository.GetChildren<ContentFolder>(_siteDefinition.GlobalAssetsRoot)
                        .FirstOrDefault(c => c.Name.Equals(ImageFolder));
                    if (mediaFolder == null)
                    {
                        mediaFolder = _contentRepository.GetDefault<ContentFolder>(_siteDefinition.GlobalAssetsRoot);
                        mediaFolder.Name = ImageFolder;
                        _contentRepository.Save(mediaFolder, SaveAction.Publish, AccessLevel.NoAccess);
                    }
                    _mediaRoot = mediaFolder.ContentLink;
                }
                return _mediaRoot;
            }
        }

        private ContentReference PageRoot => SiteDefinition.Current.StartPage?.ProviderName != null
            ? SiteDefinition.Current.StartPage
            : SiteDefinition.Current.RootPage;
    }
}