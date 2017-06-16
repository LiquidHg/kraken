//using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kraken.SharePoint.Client.Enums {

  /// <summary>
  /// Class created to manage local content type ID constants,
  /// since the CSOM ContentTypeId doesn't let us set any properties. 
  /// </summary>
  public class ContentTypeId {

    public ContentTypeId(string value) {
      this.StringValue = value;
    }

    public string StringValue { get; set; }
  }

  public sealed class BuiltInContentTypeId {

    private static Dictionary<ContentTypeId, string> s_dict = null;
    
    public static readonly ContentTypeId AdminTask = new ContentTypeId("0x010802");
    public static readonly ContentTypeId Announcement = new ContentTypeId("0x0104");
    public static readonly ContentTypeId BasicPage = new ContentTypeId("0x010109");
    public static readonly ContentTypeId BlogComment = new ContentTypeId("0x0111");
    public static readonly ContentTypeId BlogPost = new ContentTypeId("0x0110");
    public static readonly ContentTypeId CallTracking = new ContentTypeId("0x0100807fbac5eb8a4653b8d24775195b5463");
    public static readonly ContentTypeId Contact = new ContentTypeId("0x0106");
    public static readonly ContentTypeId Discussion = new ContentTypeId("0x012002");
    public static readonly ContentTypeId DisplayTemplateJS = new ContentTypeId("0x0101002039C03B61C64EC4A04F5361F3851068");
    public static readonly ContentTypeId Document = new ContentTypeId("0x0101");
    public static readonly ContentTypeId DocumentSet = new ContentTypeId("0x0120d5");
    public static readonly ContentTypeId DocumentWorkflowItem = new ContentTypeId("0x010107");
    public static readonly ContentTypeId DomainGroup = new ContentTypeId("0x010C");
    public static readonly ContentTypeId DublinCoreName = new ContentTypeId("0x01010B");
    public static readonly ContentTypeId Event = new ContentTypeId("0x0102");
    public static readonly ContentTypeId FarEastContact = new ContentTypeId("0x0116");
    public static readonly ContentTypeId Folder = new ContentTypeId("0x0120");
    public static readonly ContentTypeId GbwCirculationCTName = new ContentTypeId("0x01000f389e14c9ce4ce486270b9d4713a5d6");
    public static readonly ContentTypeId GbwOfficialNoticeCTName = new ContentTypeId("0x01007ce30dd1206047728bafd1c39a850120");
    public static readonly ContentTypeId HealthReport = new ContentTypeId("0x0100F95DB3A97E8046b58C6A54FB31F2BD46");
    public static readonly ContentTypeId HealthRuleDefinition = new ContentTypeId("0x01003A8AA7A4F53046158C5ABD98036A01D5");
    public static readonly ContentTypeId Holiday = new ContentTypeId("0x01009be2ab5291bf4c1a986910bd278e4f18");
    public static readonly ContentTypeId IMEDictionaryItem = new ContentTypeId("0x010018f21907ed4e401cb4f14422abc65304");
    public static readonly ContentTypeId Issue = new ContentTypeId("0x0103");
    public static readonly ContentTypeId Item = new ContentTypeId("0x01");
    public static readonly ContentTypeId Link = new ContentTypeId("0x0105");
    public static readonly ContentTypeId LinkToDocument = new ContentTypeId("0x01010A");
    public static readonly ContentTypeId MasterPage = new ContentTypeId("0x010105");
    public static readonly ContentTypeId MasterPagePreview = new ContentTypeId("0x010106");
    public static readonly ContentTypeId Message = new ContentTypeId("0x0107");
    public static readonly ContentTypeId ODCDocument = new ContentTypeId("0x010100629D00608F814dd6AC8A86903AEE72AA");
    public static readonly ContentTypeId Person = new ContentTypeId("0x010A");
    public static readonly ContentTypeId Picture = new ContentTypeId("0x010102");
    public static readonly ContentTypeId Resource = new ContentTypeId("0x01004c9f4486fbf54864a7b0a33d02ad19b1");
    public static readonly ContentTypeId ResourceGroup = new ContentTypeId("0x0100ca13f2f8d61541b180952dfb25e3e8e4");
    public static readonly ContentTypeId ResourceReservation = new ContentTypeId("0x0102004f51efdea49c49668ef9c6744c8cf87d");
    public static readonly ContentTypeId RootOfList = new ContentTypeId("0x012001");
    public static readonly ContentTypeId Schedule = new ContentTypeId("0x0102007dbdc1392eaf4ebbbf99e41d8922b264");
    public static readonly ContentTypeId ScheduleAndResourceReservation = new ContentTypeId("0x01020072bb2a38f0db49c3a96cf4fa85529956");
    public static readonly ContentTypeId SharePointGroup = new ContentTypeId("0x010B");
    public static readonly ContentTypeId SummaryTask = new ContentTypeId("0x012004");
    public static readonly ContentTypeId System = new ContentTypeId("0x");
    public static readonly ContentTypeId Task = new ContentTypeId("0x0108");
    public static readonly ContentTypeId Timecard = new ContentTypeId("0x0100c30dda8edb2e434ea22d793d9ee42058");
    public static readonly ContentTypeId UDCDocument = new ContentTypeId("0x010100B4CBD48E029A4ad8B62CB0E41868F2B0");
    public static readonly ContentTypeId UntypedDocument = new ContentTypeId("0x010104");
    public static readonly ContentTypeId WebPartPage = new ContentTypeId("0x01010901");
    public static readonly ContentTypeId WhatsNew = new ContentTypeId("0x0100a2ca87ff01b442ad93f37cd7dd0943eb");
    public static readonly ContentTypeId Whereabouts = new ContentTypeId("0x0100fbeee6f0c500489b99cda6bb16c398f7");
    public static readonly ContentTypeId WikiDocument = new ContentTypeId("0x010108");
    public static readonly ContentTypeId WorkflowHistory = new ContentTypeId("0x0109");
    public static readonly ContentTypeId WorkflowTask = new ContentTypeId("0x010801");
    public static readonly ContentTypeId XMLDocument = new ContentTypeId("0x010101");
    public static readonly ContentTypeId XSLStyle = new ContentTypeId("0x010100734778F2B7DF462491FC91844AE431CF");

    private BuiltInContentTypeId() {
    }

    public string GetName(ContentTypeId contentTypeId) {
      EnsureItems();
      string name;
      if (s_dict.TryGetValue(contentTypeId, out name))
        return name;
      return string.Empty;
    }

    private static void EnsureItems() {
      if (s_dict == null) {
        s_dict = new Dictionary<ContentTypeId, string>(0x36);
        s_dict[Discussion] = "Discussion";
        s_dict[ScheduleAndResourceReservation] = "ScheduleAndResourceReservation";
        s_dict[ResourceGroup] = "ResourceGroup";
        s_dict[UDCDocument] = "UDCDocument";
        s_dict[Item] = "Item";
        s_dict[SharePointGroup] = "SharePointGroup";
        s_dict[Issue] = "Issue";
        s_dict[WikiDocument] = "WikiDocument";
        s_dict[GbwCirculationCTName] = "GbwCirculationCTName";
        s_dict[HealthRuleDefinition] = "HealthRuleDefinition";
        s_dict[Announcement] = "Announcement";
        s_dict[WebPartPage] = "WebPartPage";
        s_dict[DublinCoreName] = "DublinCoreName";
        s_dict[WhatsNew] = "WhatsNew";
        s_dict[AdminTask] = "AdminTask";
        s_dict[Schedule] = "Schedule";
        s_dict[Whereabouts] = "Whereabouts";
        s_dict[DocumentWorkflowItem] = "DocumentWorkflowItem";
        s_dict[Resource] = "Resource";
        s_dict[MasterPagePreview] = "MasterPagePreview";
        s_dict[DomainGroup] = "DomainGroup";
        s_dict[Document] = "Document";
        s_dict[BlogComment] = "BlogComment";
        s_dict[HealthReport] = "HealthReport";
        s_dict[XMLDocument] = "XMLDocument";
        s_dict[Event] = "Event";
        s_dict[IMEDictionaryItem] = "IMEDictionaryItem";
        s_dict[WorkflowHistory] = "WorkflowHistory";
        s_dict[ODCDocument] = "ODCDocument";
        s_dict[Message] = "Message";
        s_dict[GbwOfficialNoticeCTName] = "GbwOfficialNoticeCTName";
        s_dict[ResourceReservation] = "ResourceReservation";
        s_dict[DisplayTemplateJS] = "DisplayTemplateJS";
        s_dict[Person] = "Person";
        s_dict[SummaryTask] = "SummaryTask";
        s_dict[XSLStyle] = "XSLStyle";
        s_dict[MasterPage] = "MasterPage";
        s_dict[CallTracking] = "CallTracking";
        s_dict[Folder] = "Folder";
        s_dict[Timecard] = "Timecard";
        s_dict[BlogPost] = "BlogPost";
        s_dict[System] = "System";
        s_dict[Picture] = "Picture";
        s_dict[RootOfList] = "RootOfList";
        s_dict[Link] = "Link";
        s_dict[Task] = "Task";
        s_dict[UntypedDocument] = "UntypedDocument";
        s_dict[Holiday] = "Holiday";
        s_dict[Contact] = "Contact";
        s_dict[FarEastContact] = "FarEastContact";
        s_dict[BasicPage] = "BasicPage";
        s_dict[WorkflowTask] = "WorkflowTask";
        s_dict[DocumentSet] = "DocumentSet";
        s_dict[LinkToDocument] = "LinkToDocument";
      }
    }

    public static bool Contains(ContentTypeId contentTypeId) {
      EnsureItems();
      string name;
      return (s_dict.TryGetValue(contentTypeId, out name));
    }
  }
}
