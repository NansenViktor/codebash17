namespace CodeBash2017.Business.Internal
{
    /// <summary>
    /// A class used to deserialize the JSON response from Bing's Computer Vision API.
    /// https://www.microsoft.com/cognitive-services/en-us/computer-vision-api
    /// </summary>
    public class BingComputerVisionResponse
    {
        // Always included
        public string RequestId { get; set; }
        public MetadataObject Metadata { get; set; }

        // In case of an error response
        public string StatusCode { get; set; }
        public string Message { get; set; }

        // Properties that are only filled in if the request includes them as "visualFeatures".
        public Category[] Categories { get; set; }
        public Tag[] Tags { get; set; }
        public DescriptionObject Description { get; set; }
        public Face[] Faces { get; set; }
        public ImageTypeObject ImageType { get; set; }
        public ColorObject Color { get; set; }
        public AdultObject Adult { get; set; }

        // Classes representing the JSON objects

        public class MetadataObject
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string Format { get; set; }
        }

        public class Category
        {
            public string Name { get; set; }
            public double Score { get; set; }
            public DetailObject Detail { get; set; }

            public class DetailObject
            {
                public Celebrity[] Celebrities { get; set; }

                public class Celebrity
                {
                    public string Name { get; set; }
                    public double Confidence { get; set; }
                    public Face.FaceRectangleObject FaceRectangle { get; set; }
                }
            }
        }

        public class Tag
        {
            public string Name { get; set; }
            public double Confidence { get; set; }
        }

        public class DescriptionObject
        {
            public string[] Tags { get; set; }
            public Caption[] Captions { get; set; }

            public class Caption
            {
                public string Text { get; set; }
                public double Confidence { get; set; }
            }
        }

        public class Face
        {
            public int Age { get; set; }
            public string Gender { get; set; }
            public FaceRectangleObject FaceRectangle { get; set; }

            public class FaceRectangleObject
            {
                public int Left { get; set; }
                public int Top { get; set; }
                public int Width { get; set; }
                public int Height { get; set; }
            }
        }

        public class ImageTypeObject
        {
            public int ClipArtType { get; set; }
            public int LineDrawingType { get; set; }
        }

        public class ColorObject
        {
            public string DominantColorForeground { get; set; }
            public string DominantColorBackground { get; set; }
            public string[] DominantColors { get; set; }
            public string AccentColor { get; set; }
            public bool IsBWImg { get; set; }
        }

        public class AdultObject
        {
            public bool IsAdultContent { get; set; }
            public bool IsRacyContent { get; set; }
            public double AdultScore { get; set; }
            public double RacyScore { get; set; }
        }
    }
}