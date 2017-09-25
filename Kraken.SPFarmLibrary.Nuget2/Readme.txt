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

v0.2.15: Ensures latest updates all underlying WSP and DLL versions
v0.2.14: Fix - KLS TryGetLocal caught in infinite recovery loop due to SecurityException under very rare pre-login circumstances.
v0.2.13: Fix - KLS CreateNew fail-safes have been reengineered to prevent circular references in claim providers.
v0.2.12: Small tweaks to KLS to reduce how often it needs to use fail-safe log recovery mode; now reference Kraken.SPSandboxLibrary via Nuget instead of project reference; AssemblyVersion compiler directives reversed to make v4.5/v15.0 the default and v3.5/v14 only when .NET 3.5 is set.
v0.2.11: Fix - adjusting methods on KLS CreateNew to account for certain contingencies in limited permission calls.
v0.2.10: Oops forgot to publish core lib before this one. :-()
v0.2.9: Updating nuget library dependencies to latest.
v0.2.8: Rebuild after discovering 4 projects weren't set to Build in the build configuration for 15-45.
v0.2.7: Trying to get .NET framework builds to load consistentkly for different build configs.
v0.2.6: Changed KLS to accept LoggingProperties in CreateNew and other methods, instead of multiple parameters.
v0.2.5: Fix - KLS add better exception handling for Register and Unregister service.
v0.2.4: Fix - KLS second attempt to fix SPDuplicateObjectException and minor fix for some backup log messaages that were incorrectly suppressed.
v0.2.3: Fix - KLS fixed to work around issues where creating a service with a duplicate name for one that exists in the config db.
v0.2.2: Fix - Minor tweaks to KLS to avoid "recovery" when it is the first try
v0.2.1: Fixes and enhancements to improve reliability of KrakenLoggingService in low-permission use cases
v0.2.0: Updating all core libs to same nuget release number
v0.1.5: Moved WSP packages and PowerShell to lib\Deploy because they weren't being included in the package when in content\Deploy
v0.1.4: Correct a bad SharePoint dependency nuget package name.
v0.1.3: Bring SPFarmLibrary current with latest release of Kraken.SharePoint.Client. Make core and sandbox libs reference from the nuget package instead of projects; added package dependencies.
v0.1.2: Certificate utility updates; some improvements to handling of user profile manager; fixed flase-flag errors on install of Logging Categories to registry for machines that are not actually SharePoint servers (SMTP/SQL);
v0.1.1: Updates to nuget packages to keep pace with consumer libs; 
v0.1.0: Initial release