using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

using Buddy.Coroutines;

using Clio.Utilities;
using Clio.XmlEngine;

using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using ff14bot.Enums;
using ff14bot.Helpers;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("Desynth")]
	public class DesynthTag : ProfileBehavior
	{
		private bool             _done = false;
		private InventoryBagId[] _bagIds = null;
		
		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }
		
		[XmlAttribute("BagIds")]
		public string BagIds { get; set; }
		
  		[DefaultValue(6000)]
		[XmlAttribute("DesynthDelay")]
		public int DesynthDelay { get; set; }
		
  		[DefaultValue(10)]
		[XmlAttribute("DesynthTimeout")]
		public int DesynthTimeout { get; set; }
		
  		[DefaultValue(3)]
		[XmlAttribute("ConsecutiveDesynthTimeoutLimit")]
		public int ConsecutiveDesynthTimeoutLimit { get; set; }
		
		private static readonly Color Info = Color.FromRgb(64, 224, 208);
		private static readonly Color Error = Color.FromRgb(255, 0, 0);
		
        new public static void Log(Color color,string text, params object[] args)
        {
            text = "[Desynth] " + string.Format(text, args);
            Logging.Write(color, text);
        }
		
		public void GetBagIds()
		{
			if (BagIds != null)
			{
				string[] bagIds = BagIds.Split(',');
				
				if (bagIds.Count() > 0)
				{
					List<InventoryBagId> bagIdList = new List<InventoryBagId>();
					
					foreach (string bagId in bagIds)
					{
						try
						{
							bagIdList.Add(
								(InventoryBagId)Enum.Parse(typeof(InventoryBagId),bagId));
						}
						catch (ArgumentException)
						{
							Log(Error,"{0} is not a member of the InventoryBagId enumeration.",bagId);
						}
					}
					
					_bagIds = bagIdList.ToArray();
				}
			}
		}
		
		protected override void OnStart()
		{
			_done = false;
			
			GetBagIds();
		}
		
        public override bool IsDone 
		{ 
			get 
			{ 
				return _done; 
			} 
		}
		
        protected override void OnDone()
        {
		}
		
		protected override Composite CreateBehavior() 
		{
			return
				new Decorator(
					ret => !_done,
					new ActionRunCoroutine(r => Desynth()));
         }
		
        protected override void OnResetCachedDone()
		{
			_done = false;
		}
		
		protected async Task<bool> Desynth()
		{
			if (!Core.Player.DesynthesisUnlocked)
			{
				Log(Error,"You have not unlocked the desynthesis ability.");
				
				return _done = true;
			}
			
			Log(Info,"Your {0}'s desynthesis level is: {1}.",Core.Player.CurrentJob,Core.Player.GetDesynthesisLevel(Core.Player.CurrentJob));
			
			IEnumerable<BagSlot> desynthables = null;
			
			if (_bagIds != null && ItemIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(_bagIds,e=>e==bs.BagId) && Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else if (_bagIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(_bagIds,e=>e==bs.BagId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else if (ItemIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else
			{
				Log(Error,"You didn't specify anything to desynthesize.");
				
				return _done = true;
			}
						
			var numItems = desynthables.Count();
						
			if ( numItems == 0)
			{
				Log(Error,"None of the items you requested can be desynthesized.");
				
				return _done = true;
			}
			else
			{
				Log(Info,"You have {0} bag slots that are desynthesizable.",numItems);
			}
			
			var itemIndex = 1;
						
			foreach (var bagSlot in desynthables)
			{
				var name = bagSlot.EnglishName;
				var stackSize = bagSlot.Count;
				var stackIndex = 1;
				var consecutiveTimeouts = 0;
				
				Log(Info,"You have {0} items in bag Slot {1} to desynthesize.",stackSize,itemIndex);
				
				while (bagSlot.Count > 0)
				{
					var desynthTarget = "item \""+name+"\"["+itemIndex+"]["+(stackIndex++)+"] of ("+numItems+","+stackSize+")";
					
					Log(Info,"Attempting to desynthesize {0} - success chance is {1}%.",desynthTarget,await CommonTasks.GetDesynthesisChance(bagSlot));

					var currentStackSize = bagSlot.Item.StackSize;
					var result = await CommonTasks.Desynthesize(bagSlot,DesynthDelay);
					
					if (result != DesynthesisResult.Success)
					{
						Log(Error,"Unable to desynthesize {0} due to {1} - moving to next bag slot.",desynthTarget,result);
						
						goto AbortDesynth;
					}
				
					await Coroutine.Wait(DesynthTimeout*1000, () => (!bagSlot.IsFilled || !bagSlot.EnglishName.Equals(name) || bagSlot.Count != currentStackSize));
				
					if (bagSlot.IsFilled && bagSlot.EnglishName.Equals(name) && bagSlot.Count == currentStackSize)
					{
						consecutiveTimeouts++;
						
						Log(Error,"Timed out awaiting desynthesis of {0} ({1} seconds, attempt {2} of {3}).",desynthTarget,DesynthTimeout,consecutiveTimeouts,ConsecutiveDesynthTimeoutLimit);
						
						if (consecutiveTimeouts >= ConsecutiveDesynthTimeoutLimit)
						{
							Log(Error,"While desynthesizing {0), exceeded consecutive timeout limit - moving to next bag slot.",desynthTarget);
							
							goto AbortDesynth;
						}
					}
					else
					{
						Log(Info,"Desynthed {0}: your {1}'s desynthesis level is now {2}.",desynthTarget,Core.Player.CurrentJob,Core.Player.GetDesynthesisLevel(Core.Player.CurrentJob));
						
						consecutiveTimeouts = 0;
					}
				}
				AbortDesynth:;
				itemIndex++;
			}
			
			return _done = true;
		}
	}
}
