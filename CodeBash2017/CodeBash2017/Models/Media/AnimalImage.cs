using System.Collections.Generic;
using EPiServer.Cms.Shell.UI.ObjectEditing.EditorDescriptors;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Framework.Serialization;
using EPiServer.Framework.Serialization.Internal;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;

namespace CodeBash2017.Models.Media
{
    /// <summary>
    /// Represents an image of an animal as analyzed by Microsoft Cognitive Services
    /// See also: https://westus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fa
    /// </summary>
    [ContentType(GUID = "11fa5ac8-90a5-44a2-afa8-bb1c480aad28")]
    [MediaDescriptor(ExtensionString = "jpg,jpeg,jpe,ico,gif,bmp,png")]
    public class AnimalImage : ImageData
    {
        /// <summary>
        /// Metadata about the image.
        /// Format: e.g. "jpeg"
        /// </summary>
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public virtual string Format { get; set; }

        /// <summary>
        /// In case of an error response
        /// </summary>
        public virtual string ErrorCode { get; set; }
        public virtual string ErrorMessage { get; set; }

        /// <summary>
        /// Categories - categorizes image content according to a taxonomy defined in documentation.
        /// Includes: Celebrities - identifies celebrities if detected in the image.
        /// </summary>
        [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<Category>))]
        public virtual IList<Category> Categories { get; set; }

        /// <summary>
        /// Tags - tags the image with a detailed list of words related to the image content.
        /// </summary>
        [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<Tag>))]
        public virtual IList<Tag> Tags { get; set; }

        /// <summary>
        /// Description - describes the image content with a complete English sentence.
        /// </summary>
        [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<Caption>))]
        public virtual IList<Caption> DescriptionCaptions { get; set; }
        public virtual string[] DescriptionTags { get; set; }

        /// <summary>
        /// Faces - detects if faces are present. If present, generate coordinates, gender and age.
        /// </summary>
        [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<Face>))]
        public virtual IList<Face> Faces { get; set; }

        /// <summary>
        /// ImageType - detects if image is clipart or a line drawing.
        /// ClipartType: Non-clipart = 0, ambiguous = 1, normal-clipart = 2, good-clipart = 3
        /// LineDrawingType: Non-LineDrawing = 0, LineDrawing = 1
        /// </summary>
        public virtual int ClipArtType { get; set; }
        public virtual int LineDrawingType { get; set; }

        /// <summary>
        /// Color - determines the accent color, dominant color, and whether an image is black & white.
        /// </summary>
        public virtual string DominantColorForeground { get; set; }
        public virtual string DominantColorBackground { get; set; }
        public virtual string[] DominantColors { get; set; }
        public virtual string AccentColor { get; set; }
        public virtual bool IsBWImg { get; set; }

        /// <summary>
        /// Adult - detects if the image is pornographic in nature (depicts nudity or a sex act). Sexually suggestive content is also detected.
        /// </summary>
        public virtual bool IsAdultContent { get; set; }
        public virtual double AdultScore { get; set; }
        public virtual bool IsRacyContent { get; set; }
        public virtual double RacyScore { get; set; }
    }

    public class PropertyListBase<T> : PropertyList<T>
    {
        public PropertyListBase()
        {
            _objectSerializer = _objectSerializerFactory.Service.GetSerializer("application/json");
        }
        private Injected<ObjectSerializerFactory> _objectSerializerFactory;

        private readonly IObjectSerializer _objectSerializer;
        protected override T ParseItem(string value)
        {
            return _objectSerializer.Deserialize<T>(value);
        }

        public override PropertyData ParseToObject(string value)
        {
            ParseToSelf(value);
            return this;
        }
    }

    public class Category
    {
        public virtual string Name { get; set; }
        public virtual double Score { get; set; }

        [EditorDescriptor(EditorDescriptorType = typeof(CollectionEditorDescriptor<Celebrity>))]
        public virtual IList<Celebrity> Celebrities { get; set; }
    }
    
    public class Celebrity
    {
        public virtual string Name { get; set; }
        public virtual double Confidence { get; set; }
        public virtual FaceRectangleObject FaceRectangle { get; set; }
    }

    public class Tag
    {
        public virtual string Name { get; set; }
        public virtual double Confidence { get; set; }
    }    

    public class Caption
    {
        public virtual string Text { get; set; }
        public virtual double Confidence { get; set; }
    }
    
    public class Face
    {
        public virtual int Age { get; set; }
        public virtual string Gender { get; set; }
        public virtual FaceRectangleObject FaceRectangle { get; set; }
    }

    public class FaceRectangleObject
    {
        public virtual int Left { get; set; }
        public virtual int Top { get; set; }
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
    }

    [PropertyDefinitionTypePlugIn]
    public class FaceListProperty : PropertyListBase<Face>
    {

    }

    [PropertyDefinitionTypePlugIn]
    public class StringListProperty : PropertyListBase<string>
    {

    }

    [PropertyDefinitionTypePlugIn]
    public class CategoryListProperty : PropertyListBase<Category>
    {

    }

    [PropertyDefinitionTypePlugIn]
    public class TagListProperty : PropertyListBase<Tag>
    {

    }

    [PropertyDefinitionTypePlugIn]
    public class CelebrityListProperty : PropertyListBase<Celebrity>
    {

    }

    [PropertyDefinitionTypePlugIn]
    public class CaptionsListProperty : PropertyListBase<Caption>
    {

    }
}