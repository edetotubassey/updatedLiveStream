namespace com.tiledmedia.clearvr.demos {
/// <summary>
/// Model for the content items used on app level.
/// We add some extra information to each content item to make the main menu look better.
/// These fields are not provided by  Tiledmedia and need to be retrieved by your own content management system.
/// Use the ContentItem to interface with the Tiledmedia SDK.
/// This is purely for demonstration purposes.
/// </summary>
    public class ContentItemModel {
        private string _description;
        private ContentItem _clearVRContentItem;
        public string description {
            get { return _description; }
            private set { _description = value; }
        }
        public ContentItem clearVRContentItem {
            get { return _clearVRContentItem; }
            private set { _clearVRContentItem = value; }
        }

        public ContentItemModel(string description, ContentItem contentItem) {
            this._description = description;
            this._clearVRContentItem = contentItem;
        }
    }
}