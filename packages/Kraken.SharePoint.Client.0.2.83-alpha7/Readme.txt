Hello, and welcome to the Kraken library for SharePoint.

We've been building this library for seven years now, assuming
you don't count Behemoth which was our open source library from
the SharePoint 2007 era.

So, the code library is pretty big! You might wonder where to begin.

First, most of our extension methods now piggyback onto the CSOM
namespace Microsoft.SharePoint.Client, so if you're using CSOM code
you'll automatically start to see our extensions show up for various
aspects of those classes.

Apologies that documentation is a bit sparse. We do our best to comment
the code and to develop examples and such, but it's a big fight for such
a tiny little company. We're doing our best.

If you're stuck trying to figure it out, reach out to us at 
support@liquidmercurysolutions.com and we'll send you a few examples.

Here are five things you can do to show your support:

/***
#1 Give us a 5 star review on Google:
***/
You can use the link below to give us a good review.
Write a Review is shown on our company profile in the
right-hand column.
http://GoogleLiquidHgWashDC.easyurl.net

Providing a review for our DC location helps us the most,
because DC is the biggest market for SharePoint work in our
area and also the biggest user base (so far) for Kraken.

If you have a negative criticism, please reach out to us and
give us the opportunity to make it right before you post a bad
review. These things can really hurt and we all have our jobs
depending on this. Not being able to eat or pay our mortgages
would be really out of proportion with our code having a few bugs.

/***
#2 Buy us a pizza or some beer.
***/
Did you find Kraken to be really valuable to a SharePoint project
you were working on? We're not against taking a donation if you 
want to show your support.

Funds will be used directly to support the developers who helps make 
Kraken a reality. We don't expect to make tons of money this way, so
whatever you contribute will buy nice things to feed our programmers,
have a nice party, and that kind of stuff. If you donate, we promise to
send you a thank you letter and tell you exactly what we did with the
money - probably with pictures. ^_^

We have a paypal account:
http://donateliquidhgpaypal.easyurl.net/

We're also currently looking into Patrion and whether code qualifies as a work of art.

/***
#3 Visit our Blog and leave a comment.
***/
Writing blogs is hard work, with little reward.
Please tell our writers that you found the updates helpful.

/***
#4 Visit our Google+ page and +1 our posts.
***/
Liquid Mercury is still one of the best kept secrets in
the Washington DC area. Help us get the word out - so we
can get some work.
https://plus.google.com/+LiquidmercurysolutionsSharePoint
Or visit us on LinkedIn:
https://www.linkedin.com/company/liquid-mercury-solutions

/***
#5 You can help us make Kraken even better! 
***/
Find Kraken on GitHub at https://github.com/LiquidHg/kraken 
and ask us to contribute to the project. Contact Tom Carpe at 
thomas.carpe@liquidmercurysolutions.com to let us know you want to 
join the team.

Update History:

v0.2.83-alpha7: Experimental build to make it possible to set NavigateForFormsPages and Flags in Lists using legacy web service calls. The process failed for these two properties, but worked for 4 others that were previously inaccessible. We also added some of the properties now available through the latest version of CSOM.v0.2.82: Expanded ListOptions and List.Update() to include DefaultContentType and DocumentTemplateType is now [better] implemented.
v0.2.82: ???
v0.2.81: Improvements to ListOptions to include more properties for list Create() and Update() methods.
v0.2.80: Fixed a bug in List.Update() extension that would cause core properties to be skipped if there were no extended properties being set.
v0.2.79: Core updated to package 0.2.5; Global nuget dependency updates including log4net 2.0.7
v0.2.78: Moved TraceExtensions from Core.FullTrust to Core.SandboxTrust. Add reference in SandboxTrust project to Core.Security.
v0.2.77: Experimental - brought in a bunch of utility classes that were developed in CloudPower but may be useful elsewhere. Added reflection based property copy to ParsableOptions.
v0.2.76: Added function to LookupFieldProvisioner to help us easily get all the additional fields for a primary lookup field.
v0.2.75: Added several shorthand methods to FieldProperties in an effort to make it easier to interact with.
v0.2.74: Changes to site column "get/find" extensions to allow for recurse parent webs, user fields only, and move core logic to field collection extensions.
v0.2.73: Added method to detect user-created vs. built-in fields/site columns.
v0.2.72: Added support for dynamic indentation to the ITrace interface and implementation.
v0.2.71: Skip fields that already exist when creating auxiliary lookup fields in LookupFieldProvisioner.
v0.2.70: Fix: Logic in FieldCollection::CanonicalizeFormula was causing it to be executed twice.
v0.2.69: Very minor edits to verbose trace output in order to try and make things a bit cleaner to read.
v0.2.68: Experimental: Code added to add/update column by properties that will canonicalize the formulas provided and add FieldRefs as needed to prevent errors down the road.
v0.2.67: Fix: Eliminate redundant warnings in LookupFieldProvisioner when a field doesn't exist that erroneously says it isn't supported type either.
v0.2.66: Experimental: Trying to isolate issues in ContentTypeExtensions causing a rare NullReferenceException that only occurs in certain cases.
v0.2.65: Very minor improvements to the way fields are created/updated, meant to improve the provisioning process.
v0.2.64: Fix: This improves the way that LookupFieldProvisioner tells the difference betweena field that does not exist in a List and a field that isn't supported; attempts to tell the user what's-what. Permits more field types than before including Integer, Boolean, Currency, and Choice.
v0.2.63: Fix: First attempt to fix issue in LookupFieldProvisioner where getting the target list comes back with 'cannot complete this action' after the List has a content type added to it. This is really more of a diagnostic build with process broken out so we can isolate the problem.
v0.2.62: Fix: Minor bug in ContentTypeCache causes webs or lists which have not had their Id property explicityly loaded to throw an exception. We now check for this before runnning linq queries.
v0.2.61: Fix: Third try to fix issue from v0.2.58.
v0.2.60: Fix: bug in Web.CreateList from ListProperties that stopped it from running List.Update which meant that any extended properties and content types were not handled on add.
v0.2.59: Fix: Build v0.2.58 had some shortcomings; attempting a different methodlogy for filtering support and provided lookup in fieldsLookupFieldProvisioner.
v0.2.58: Fix: LookupFieldProvisioner had issue finding additional lookup fields because it was only searching by Title; added InternalName to the query.
v0.2.57: Fix: LookupFieldProvisioner was not equipped to handle updates to existing lookup fields, not was it attached to extension methods for update of fields/site columns.
v0.2.56: Fix: LookupFieldProvisioner would fail to find the list if it wasn't a doclib, most likely due to presence of 'Lists/' in the URL.
v0.2.55: Fix: ListProperties and List::Update extension were not respecting [SKIP_PROPERTY] for setting the default view, resulting in errors when adding or updating list.
v0.2.54: Added smart property DefaultValueOrFormula to FieldProperties, which will set Formula, DefaultFormula, or DefaultValue automagically.
v0.2.53: Fix: Left Id out of ContentTypeProperties copy method, resulting in creation of content types with randomly generated Ids.
v0.2.52: Trap and rethrow exception where trying to create a content type with same ID in a different Web will cause an InvalidOperationException without good explanation why.
v0.2.51: Added support for props.EnsureContentType[] to List.Update() extension method.
v0.2.50: Added ListOptions to allow for a broader array of properties when creating/updating Lists + Web.CreateList() and List.Update() extensions.
v0.2.49: Fix - Remove "URL" from the default set of standard view fields, because creation of Views blew up where List queries dsomehow allowed it.
v0.2.48: Fix - stupid boolean flipflop in null detection of AddEnsureFieldsToOrderBy
v0.2.47: Multiple functions added to CamlHelpers to systematize the creation of View CAML for use across multiple consumers.
v0.2.46: View.Update extended to include setting as the default view, provie a list name for debugging purposes.
v0.2.45: Added additional properties to ViewProperties and ContentTypeProperties; added Update extension method for View.
v0.2.44: Fix - SecureStringMarshaller would crash on Decrypt if the secure string length was zero. Added "ServerRelativeUrl" as an alias to "ServerUrl" in built-in fields; added ServerUrl to the default document view fields to return. Added additional null checking to EnsureContentTypes.
v0.2.43: Fix - CAML.Where() for field-op-value in GetItems/GetItemsNoPaging/GetItemsPage/GetLookupItem were all flawed, resulting in queries which always return 0 items. Value was used for field name instead of FieldRef; now using correct syntax.
v0.2.42: Packaging glitch; version++ bump.
v0.2.41: Fix - GetItemNoPaging now uses the same query execution logic and exception handling as GetItemsPage, so they should be more consistent.
v0.2.40: Fix - GetItemNoPaging "value does not fall within the expected range" on attempt to call EnsureProperty, which does ExecuteQuery. testing...
v0.2.39: Fix - GetLookupItem would not return a match if the item was in a folder, because scope was All instead of RecursiveAll. Changed logic for underlying call to GetItems/GetItemsNoPaging.
v0.2.38: Added property validation to CamlFieldToValueMatchOptions.
v0.2.37: Fix - additional boolean logic + nullable types related to v0.2.34 with setting of Id and ListItemIdentifier.SimpleMatch
v0.2.36: Moved QueryItemOptions to library and implemented it in GetItems list extensions; refactored several methods to make pagination the default option when querying list items.
v0.2.35: Fix - additional issue related to v0.2.34 with setting of Id and ListItemIdentifier.HasIdentiyingProperty
v0.2.34: Fix - corrected boolean logic + nullable types bug in ListItemIdentifier.HasIdentiyingProperty which caused it to return true even when values were not present
v0.2.33: Fix - not providing an operator for CAML query (empty string) causing error; now defaults to "Eq"
v0.2.32: Fix - adjusted some of the trace logs in CamlHelpers so that view field conversion is not logged more times than necessary.
v0.2.31: After issues with stuck DLLs in the GAC, AssemblyVersion is now 4.5.* where 4.5 matches the terget framework; AssemblyFileVersion will be 4.5.YYMM.rev as needed (but is updated maually); this change effects only Kraken.Core, Kraken.Core.FullTrust, and Kraken.Security. CredUtils methods now default to throwOnError = true.
v0.2.30: Reordered some params and improve parameter checking on CredWrite
v0.2.29: Added CredUtil managed overloads to CredRead and CredWrite which are ports from PowerShell with better error checking and usability.
v0.2.28: Added embedded c# code from CredMan.ps1 to Kraken.Core.Security so that we can call it directly from WebContextManager to save creds for future use.
v0.2.27: Added class ParsableOptions with collection for reporting parse errors; made classes that have Hashtable constructors derive from it. Added logging to report fields returned from GetItemsWithPaging. Refactored named of GetAllItems and GetItemsWith... extension methods.
v0.2.26: Fix - Disconnected trace params in some overloads of GetAllItemsWithPaging; CamlFieldToValueMatchOptions hashtable constructor had invalid foreach loops that would cause type conversion errors if used.
v0.2.25: Fix - CamlHelper had a couple places where ITrace trace was null and shoul dhave defaulted to NullTrace.Default to prevent NullReferenceException.
v0.2.24: Extended ListItemIdentifier to include a multi-purpose item finder with intent to leverage this is pipe binders and other find utilities downstream.
v0.2.23: Moved SimpleTarget/SimpleMatch to ListItemIdentifier from CamlMatchOptions.
v0.2.22: Added IListItemIdentifier and methods to support uniform retrieval of items by their identifying characateristics to CamlMatchOptions (not sure if this is the best perma-home; maybe it belongs in ListExtensions).
v0.2.21: Added KrakenHashtableExtensions class to simplify some conversions needed for converting field keys to string arrays.
v0.2.20: Extended CamlFieldToValueMatchOptions to all properties to be parsed and set one at a time use SetProperty instead of only from entire Hashtable.
v0.2.19: Support "MatchOptions." prefix in hash table imports for CamlFieldToValueMatchOptions
v0.2.18: Implemented [KEEP_VALUE] token for UpdateItem; Removed unused context manager parameter from one overload. Implemented update options for do not overwrite any existing metadata and ignoring whitespace that would wipe out data.
v0.2.17: Version bump.
v0.2.16: SimpleMatch added to CamlFieldToValueMatchOptions to assist with memory based queries using Linq.
v0.2.15: Adjusted UpdateItem and TrySetFieldValue to accept UpdateItemOptions in all cases and work with both Dictionary and Hastable. Marked some methods obsolete.
v0.2.14: CamlHelpers.ResolveViewFields has been extended to allow a list of fields to ensure are added to those requested by the caller.
v0.2.13: Add additional setting to UpdateItemOptions to allow us to disable overwrite existing data.
v0.2.12: Added classes to convert json-style hash tables into CAML where clause for querying lists.
v0.2.11: Correct a disconnected trace parameter in GetAllItemsWithPaging that buried some of the diagnostic informaiton in list queries.
v0.2.10: Added CamlHelpers.ConvertToOrderBy to convert Hashtable to strong type for CAML query order by clause.
v0.2.9: Added params to GetItemsWithPaging to support orderBy in queries.
v0.2.8: Call ctx.Site.EnsureProperty for site.Url in GetUrl added in v0.2.7.
v0.2.7: Implement GetUrl method for ListItem extension to simplify collection and display of various URL types for both items and documents.
v0.2.6: Refinements to CamlHelper's GetDefaultQueryFields and ResolveQueryFields that will allow more control when you need to add your own fields into the collection.
v0.2.5: Added "URL" to fields queried by default; use built-in fields class constants instead of hard coded string sin CamlHelper class.
v0.2.4: Added GetDefaultQueryFields helper to CamlHelper to auto-determine default fields based on list or doclib.
v0.2.3: All extension classes have been moved to Microsoft.SharePoint.Client namespace and prefixed with Kraken so they do not conflict with extension classes in OfficeDevPnp libraries. Added a couple handy constants to CamlHelper for populating default fields in queries.
v0.2.2: With apologies to all, v0.2-0.2.1 had incorrect .net framework references in the build; this version includes corrections for 4.0/4.5 and also recompiles the older 3.5 version for the first time in a while.
v0.2.1: Release to nuget to fix an issue caused when we pushed using API v2
v0.2.0: Introduces additional overloads for getting all items from a list with pagination. Updated code projects to VS2015 Update 3. See project web site for for information about past updates.
v0.1.57: [TODO look this up from nuget and fill in here.]
v0.1.56: [TODO look this up from nuget and fill in here.]
v0.1.55: Fix: OK I did something boneheaded and included the field properties without loading the collection.
v0.1.54: Fix: The collection has not been initialized. in UpdateItem because now somehow we are asking for field internal name before ever getting it from CSOM. Must've rubbed something out during the refactoring that included consolidation of several executeQuery and introduction of ExceptionHandlingScope.
v0.1.53: Fix: NullReferenceException on updateitem because null check and operation were transposed in code; added a null check and warning to LoadProperties method just in case.
v0.1.52: Experimental: Combine createitem from 2 callbacks to one in order to resolve property not loaded. Still calls csom 2nd time for extended values in the property hash table.
v0.1.51: Experimental: rewriting some ExceptionHandlingScope so they are properly implemented, added correct method to capture error messages.
v0.1.50: Experimental: rolling back some exception handling scope for item creation because it was conflicting withscopes in other methods
v0.1.49: Fix: Corrects a bug introduced in 0.1.45 that CreateItem/UpdateItem did not properly load BaseType because of a bug where it tried to get this from item instead of list. Added more verbose trace logging to item creation.
v0.1.48: Fix: Obscure issue for Calculated fields and those with a (default value that is a formula) where the presence of FriendlyDisplayFormat in the SchemaXml seems to be causing "cannot complete this action" when you try to update the field or retrieve any list property related to the 'corrupted' field schema.
v0.1.47: Experimental: Isolated problem to loading properties of a list that aren't allowed. Modified LoadAllProperties, LoadProperties, and EnsureProperty to include a debugging mode that will execute a query after each property so we can isolate which property causes the issue.
v0.1.46: Experimental: Second attempt; rewrote exception handling scope fromt eh ground up for UpdateItem.
v0.1.45: Experimental: First attempt to add exception handling scope to item add/update functions. Still getting "cannot complete this action" on call to UpdateItem but believe it is coming from an earlier CSOM call.
v0.1.44: Experimental: Testing a fix for IsDocumentLibrary where sometimes trying to load list.BaseType throw "cannot complete this action"
v0.1.43: Fix: web extension CreateOrUpdateFriendlyUrl did not propertly call context.Load for Id from the term set. Doesn't need to call context.ExecuteQuery twice. Additional logging to indicate success on finding prent friendly url's term.
v0.1.42: Fix: web extension CreateOrUpdateFriendlyUrl fails to properly return new friendly Url due to Uri formatting issue.
v0.1.41: Minor fix: web extension CreateFriendlyUrl renamed to CreateOrUpdateFriendlyUrl; now outputs Uri instead of string; output params fixed to provide a value on both create and update.
v0.1.40: New commands: Added web extensions for working with global and local navigation properties as well as creating new friendly urls in navigation term set. Changes from 0.1.39 and earlier have been tested and seem to be working OK.
v0.1.39: Experimental: Added list extension IsDocumentLibrary(); eliminated ClientContext extension Init() in favor of ClientObject extension EnsureProperty() because it's purpose is more intuitive and will often result in fewer lines of code. Made sure that BaseType is always loaded in in List and ListItem extensions UpdateItem().
v0.1.38: Experimental: Identified NullReferenceException from 0.1.37 only occurs in doc libs when Title field is empty. Adding code to use FileLeafRef instead where needed.
v0.1.37: Experimental: Fix for NullReferenceException in List extension UpdateItem.
v0.1.36: Experimental: Added same error checking logic at the List extension UpdateItem level.
v0.1.35: Experimental: Additional logging and error checking for ListItem extension item Update().
v0.1.34: Minor bugfix for Content type extension AddFieldLink; some fixed to FieldTypeAlias
v0.1.33: Content type extension AddFieldLink now loads the FieldLinks collection in content type so properties can be used properly.
v0.1.32: Fixed issue in web extention GetSiteColumn where attempt to read URL safe for SharePoint 2010 causes a CSOM error.
v0.1.31: All FieldLink creation (add Site Column to content type) extensions now return a FieldLink object on success or null on fail, saving another call to CSOM to get this later.
v0.1.30: Fixed an issue in FieldProperties affecting validation of optional nullable properties. This bug was having an impact on commands for creating and setting field and site column properties.
v0.1.29: New commands: Added GetWebTemplates extension to quickly get OOTB or custom web templates for the site collection.
v0.1.28: Fixed a bug in web extensions TryGetList that caused it to fail under certain conditions where the list does not exist but SharePoint returned a ServerException that didn't meet the evaluation criteria.
v0.1.28: Added executeQuery param to web.GetContentTypesInGroup
v0.1.27: Fix for missing property load in web.GetContentTypesInGroup; added prototype class to implement workfor extension methods
v0.1.26: Fixing logs for list extension method EnsureRemoteEvent so they correctly report the event they are attaching.
v0.1.25: Added pseudo-enum class StandardNavigationProviderNames.
