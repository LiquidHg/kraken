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
https://www.google.com/search?q=Liquid%20Mercury%20Solutions%20-%20Office%20365%20%2B%20SharePoint%201100%20N%20Glebe%20Road%2C%20Suite%201010%20Arlington&ludocid=11688250396201542100&hl=en

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
have a nice party, or that kinds of stuff. If you donate, we promise to
send you a thank you letter and tell you exactly what we did with the
money - probably with pictures. ^_^

We have a paypal account:
https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=MZSUGBA8BKUMC

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
