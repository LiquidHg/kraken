/*
  Project Kraken: SPARK for Microsoft SharePoint 2010
  Copyright (C) 2003-2011 Thomas Carpe. <http://www.ThomasCarpe.com/>
  Maintained by: <http://www.LiquidMercurySolutions.com/>

  This file is part of SPARK: SharePoint Application Resource Kit.
  SPARK projects are distributed via CodePlex: <http://www.codeplex.com/spark/>

  You may use this code for commercial purposes and derivative works, 
  provided that you maintain all copyright notices.

  SPARK is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version. You should have received a copy of
  the GNU General Public License along with SPARK.  If not, see
  <http://www.gnu.org/licenses/>.

  SPARK is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  
  We worked hard on all SPARK code, and we don't make any profit from
  sharing it with the world. Please do us a favor amd give us credit
  where credit is due, by leaving this notice unchanged. We all stand
  on the backs of giants. Wherever we have used someone else's code or
  blog article as the basis of our work, we have provided references
  to our source.
*/

namespace Kraken.SharePoint.Events {

  using System;
  using System.Security.Permissions;
  using System.IO;
  using System.Xml;
  using System.Xml.Linq;
  using System.Xml.XPath;

  using Microsoft.SharePoint;
  using Kraken.SharePoint;

  /// <summary>
  /// List Item Events
  /// </summary>
  public class ReadInfoPathListItemEventReceiver : SPItemEventReceiver {

    //public string FieldSource { get; set; } //= "CategoryTitle";
    //public string FieldTarget { get; set; } //= "DocCategoryName";

    /// <summary>
    /// An item is being added.
    /// </summary>
    public override void ItemAdding(SPItemEventProperties properties) {
      //SyncFieldValue(properties.ListItem);
      base.ItemAdding(properties);
    }

    /// <summary>
    /// An item is being updated.
    /// </summary>
    public override void ItemUpdating(SPItemEventProperties properties) {
      //SyncFieldValue(properties.ListItem);
      base.ItemUpdating(properties);
    }

    protected virtual void ExportFileDate(SPListItem docItem) {
      using (Stream docStream = docItem.File.OpenBinaryStream()) {
        bool fileIsUpdate = false;

        string fileExtension = Path.GetExtension((string)docItem.File.Item[SPBuiltInFieldId.EncodedAbsUrl]).ToUpper();
        if (fileExtension.Equals(".XML")) {
          // TODO some cool stuff where we get the fields from the XPath
          XmlReader reader = new XmlTextReader(docStream);
          XDocument doc = XDocument.Load(reader);
          XmlNamespaceManager names = new XmlNamespaceManager(reader.NameTable);
          doc.CreateNavigator().SelectSingleNode("//my:SomeElement", names);
        }
      }
    }

  } // class
} // namespace

    /*
    protected virtual void SyncFieldValue(SPListItem item) {
      base.EventFiringEnabled = false;
      string status;
      bool didRead = item.TryGetValue<string>(FieldSource, out status);
      if (didRead) {
        item[FieldTarget] = status;
        item.SystemUpdate(false);
      }
      base.EventFiringEnabled = true;
    }
     */

/*
        private void UpdateDocument(SPItemEventProperties properties) 
        { 
            SPWeb web = properties.Web; 
 
            SPListItem docItem = properties.ListItem; 
 
            string imageUrl = docItem["ImageUrl"] != null ? docItem["ImageUrl"].ToString() : ""; 
 
            if (string.IsNullOrEmpty(imageUrl)) 
                return; 
 
            using (Stream docStream = docItem.File.OpenBinaryStream()) 
            { 
                bool fileIsUpdate = false; 
 
                string fileExtension = Path.GetExtension((string)docItem.File.Item[SPBuiltInFieldId.EncodedAbsUrl]).ToUpper(); 
 
                if (fileExtension.Equals(".DOCX") || fileExtension.Equals(".DOTX") || fileExtension.Equals(".DOC") || fileExtension.Equals(".DOT")) 
                { 
                    #region Open the document file 
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(docStream, true)) 
                    { 
                        const string IMAGEPART_ID = "Acando.OpenXmlDemo.ImagePartId"; 
                        const int EMUS_PER_INCH = 914400; 
 
                        MainDocumentPart mainPart = doc.MainDocumentPart; 
 
                        ImagePart imagePart = null; 
 
                        #region Delete any previously added image 
                        try 
                        { 
                            imagePart = (ImagePart)doc.MainDocumentPart.GetPartById(IMAGEPART_ID); 
                            doc.MainDocumentPart.DeletePart(imagePart); 
                            imagePart = null; 
                        } 
                        catch { } 
                        #endregion 
 
                        SPFile imageFile = GetImageFile(imageUrl); 
 
                        if (imageFile != null) 
                        { 
                            #region Create a new image part for the specified image 
                            ImagePartType imagePartType = GetImagePartType(imageUrl); 
                            long imageWidthEMU = 0, imageHeightEMU = 0; 
 
                            imagePart = doc.MainDocumentPart.AddImagePart(imagePartType, IMAGEPART_ID); 
 
                            using (MemoryStream imageStream = new MemoryStream(imageFile.OpenBinary())) 
                            { 
                                imagePart.FeedData(imageStream); 
                            } 
 
                            using (MemoryStream imageStream = new MemoryStream(imageFile.OpenBinary())) 
                            { 
                                using (var imageBmp = new Bitmap(imageStream)) 
                                { 
                                    imageWidthEMU = (long)(imageBmp.Width * EMUS_PER_INCH / imageBmp.HorizontalResolution); 
                                    imageHeightEMU = (long)(imageBmp.Height * EMUS_PER_INCH / imageBmp.VerticalResolution); 
                                } 
                            }  
                            #endregion 
 
                            // Get the image element which will be added to the document 
                            var element = GetImageElement(mainPart.GetIdOfPart(imagePart), imageWidthEMU, imageHeightEMU); 
 
                            // Add the image element at the top of the body 
                            doc.MainDocumentPart.Document.Body.InsertAt(new Paragraph(new Run(element)), 0); 
 
                            fileIsUpdate = true; 
                        } 
 
                        if (fileIsUpdate) 
                            mainPart.Document.Save(); 
 
                        doc.Close(); 
                    } 
                    #endregion 
                } 
 
                if (fileIsUpdate) 
                { 
                    CheckInFile(docItem, docStream); 
                } 
 
                docStream.Close(); 
            } 
        } 

*/
