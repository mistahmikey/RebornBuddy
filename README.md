# RebornBuddy Goodies

Under the OrderBotTag directory you will find the following tags:

DesynthTag.cs

This tag will desynthesize specified items in your inventory. You can choose the the items by their Item Id, Bag Id, or both. If a selected bag slot contains a stack of desynthable items, it will desynthesize them one at a time.

Use:

<Desynth BagIds="<Bag Id List>" ItemIds="<Item Id List>" DesynthDelay="<Desynth Delay>" DesynthTimeout="<Desynth Timeout>" ConsecutiveDesynthTimeoutLimit="<Consecutive Desynth Timeout Limit>"/>

Where:

<Bag Id List> is a comma-separated list of inventory Bag Ids [Bag1|Bag2|Bag3|Bag4] to be desynthesized.  Only items in the matching bagswill be chosen for desynthesis. Default is "Bag1,Bag2,Bag3,Bag4".

<ItemId List> is a comma-separated list of Item Ids to be desynthesized.  Only items that match the specified Item Ids will be chosen for desynthesis.

<DesynthDelay> is the time in milliseconds to delay after the desynthesis attempt.  Default is "6000".

<Desynth Timeout> is the time in seconds to wait for the desynthesis to complete.  Default is "3".

<Consecutive Desynth Timeout Limit> is the number of consecutive desynthesis timeouts that can occur before the tag gives up and exits.  Default is "10".

If you only provide "BagIds", then the tag will attempt to desynthesize every desynthesizable item in the specified inventory Bag Ids.

If you only provide "ItemIds", then the tag will attempt to desynthesize only desynthesizable items that match the specified Item Ids across all inventory bags.

If you provide both "BagIds" and "ItemIds", then the tag will attempt to desynthesize only desynthesizable items that match the specified Item Ids within the specified inventory Bag Ids.

--------------------------------------------------------------------------------------------------------------------------------------

ConvertTag.cs

This tag will convert specified items in your inventory into materia.  You choose the items by their Item Id; only items that are actually convertable (100% Spritbonded and are marked convertable) are selected.  By default, the tag will only look for matching items in your inventory bags, but you can tell the tag to first transfer all matching items from your Armory bags into your inventory bags.

Use:



