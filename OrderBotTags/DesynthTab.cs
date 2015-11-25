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
	[XmlElement("Convert")]
	public class ConvertTag : ProfileBehavior
	{
		private bool _done = false;
		
		[DefaultValue(1)]
		[XmlAttribute("MinLevel")]
		public int MinLevel { get; set; }

		[DefaultValue(300)]
		[XmlAttribute("MaxLevel")]
		public int MaxLevel { get; set; }

		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }
		
		[DefaultValue(false)]
		[XmlAttribute("TransferConvertiblesFromArmoryToInventory")]
		public bool TransferConvertiblesFromArmoryToInventory { get; set; }

		[DefaultValue(true)]
		[XmlAttribute("ConvertInventory")]
		public bool ConvertInventory { get; set; }

  		[DefaultValue(true)]
		[XmlAttribute("AcknowledgeConversionManually")]
		public bool AcknowledgeConversionManually { get; set; }

  		[DefaultValue(10)]
		[XmlAttribute("ConversionAcknowledgementTimeout")]
		public int ConversionAcknowledgementTimeout { get; set; }

		private static readonly int   ConversionDialogOpenTimeout = 2000;
		private static readonly int   ItemTransferTimeout = 2000;
		private static DateTime       LastLogTime = DateTime.Now.Subtract(TimeSpan.FromMinutes(1.0));
		
		private static readonly Color Info = Color.FromRgb(64, 224, 208);
		private static readonly Color Error = Color.FromRgb(255, 0, 0);
		
        new public static void Log(Color color,string text, params object[] args)
        {
            text = "[Convert] " + string.Format(text, args);
            Logging.Write(color, text);
        }
		
		public static bool IsSpiritBondPossible(int[] itemIds)
		{
			if (itemIds == null)
			{
				return false;
			}
			
			var armoryConvertiblesCount   = InventoryManager.FilledArmorySlots.Where(bs=>Array.Exists(itemIds,e=>e==bs.RawItemId)&&bs.SpiritBond!=100&&bs.Item.Convertible!=0).Count();
			var remainingToSpiritbond = armoryConvertiblesCount;
			var timeSinceLastLog = DateTime.Now.Subtract(LastLogTime);
			
			if (timeSinceLastLog.Minutes >= 1)
			{
				Log(Info,"You have {0} items remaining to be spirit bonded.",remainingToSpiritbond);
				
				LastLogTime = DateTime.Now;
			}
		
			return 
				remainingToSpiritbond != 0;
		}
		
		public static bool IsSpiritBondPossible(int minLevel,int maxLevel)
		{
			var armoryConvertiblesCount   = InventoryManager.FilledArmorySlots.Where(bs=>bs.SpiritBond!=100&&bs.Item.Convertible!=0&&bs.Item.ItemLevel>=minLevel&&bs.Item.ItemLevel<=maxLevel).Count();
			var remainingToSpiritbond = armoryConvertiblesCount;
			var timeSinceLastLog = DateTime.Now.Subtract(LastLogTime);
			
			if (timeSinceLastLog.Minutes >= 1)
			{
				Log(Info,"You have {0} items remaining to be spirit bonded.",remainingToSpiritbond);
				
				LastLogTime = DateTime.Now;
			}
			
			return 
				remainingToSpiritbond != 0;
		}
		
		protected override void OnStart()
		{
			_done = false;
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
					new ActionRunCoroutine(r => Convert()));
         }
		
        protected override void OnResetCachedDone()
		{
			_done = false;
		}
		
		protected async Task<bool> ConvertInventoryIfRequested()
		{
			if (ConvertInventory)
			{
				var convertibles = 
					ItemIds != null
					?
						InventoryManager.FilledSlots.
						Where(
							bs => Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.SpiritBond == 100 && bs.Item.Convertible != 0)
					:
						InventoryManager.FilledSlots.
						Where(
							bs => bs.SpiritBond == 100 && bs.Item.Convertible != 0 && bs.Item.ItemLevel >= MinLevel && bs.Item.ItemLevel <= MaxLevel);
							
				foreach (var bagSlot in convertibles)
				{
					string name = bagSlot.EnglishName;
					
					Log(Info,"Attempting to convert \"{0}\" to materia.", name);
					
					if (!MaterializeDialog.IsOpen)
					{
						MaterializeDialog.Open(
							bagSlot);
							
						await Coroutine.Wait(ConversionDialogOpenTimeout, () => MaterializeDialog.IsOpen);
					}
					
					if (!MaterializeDialog.IsOpen)
					{
						Log(Error,"Timed out awaiting open of conversion dialog ({0} milliseconds) - unable to convert \"{1}\" to materia.",ConversionDialogOpenTimeout,name);
						
						continue;
					}
					
					if (!AcknowledgeConversionManually)
					{
						await Coroutine.Sleep(1000);
					
						MaterializeDialog.Yes();
					}
					
					await Coroutine.Wait(ConversionAcknowledgementTimeout*1000, () => !MaterializeDialog.IsOpen && (!bagSlot.IsFilled || !bagSlot.EnglishName.Equals(name)));
					
					if (MaterializeDialog.IsOpen || (bagSlot.IsFilled && bagSlot.EnglishName.Equals(name)))
					{
						Log(Error,"Timed out awaiting acknowledgement to convert \"{0}\" to materia ({1} seconds).",name,ConversionAcknowledgementTimeout);
						
						MaterializeDialog.No();
					}
					else
					{
						Log(Info,"Converted \"{0}\" to materia.", name);
					}
				}
			}
			
			return true;
		}
		
		protected async Task<bool> Convert()
		{
			if (TransferConvertiblesFromArmoryToInventory)
			{
				var sourceIt  =
					ItemIds != null
					?
						InventoryManager.FilledArmorySlots.
						Where(
							bs => Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.SpiritBond == 100 && bs.Item.Convertible != 0).
						GetEnumerator()
					:
						InventoryManager.FilledArmorySlots.
						Where(
							bs => bs.SpiritBond == 100 && bs.Item.Convertible != 0 && bs.Item.ItemLevel >= MinLevel && bs.Item.ItemLevel <= MaxLevel).
						GetEnumerator();
				
				if (!sourceIt.MoveNext())
				{
					Log(Error,"No convertible items are in your armory.");
					
					await ConvertInventoryIfRequested();
				}
				else
				{
					var inventoryBags = InventoryManager.GetBagsByInventoryBagId(InventoryBagId.Bag1,InventoryBagId.Bag2,InventoryBagId.Bag3,InventoryBagId.Bag4);
					
					while (true)
					{							
						await ConvertInventoryIfRequested();
						
						int itemsTransferred = 0;
					
						foreach (var bag in inventoryBags)
						{
							var destIt = bag.GetEnumerator();
							
							while (destIt.MoveNext())
							{
								if (!destIt.Current.IsFilled)
								{
									sourceIt.Current.Move(destIt.Current);
									
									await Coroutine.Wait(ItemTransferTimeout, () => !sourceIt.Current.IsFilled && destIt.Current.IsFilled);
									
									if (sourceIt.Current.IsFilled || !destIt.Current.IsFilled)
									{
										Log(Error,"Timed out awaiting transfer of item from armory to inventory ({0} milliseconds).",ItemTransferTimeout);
									}	
									else
									{
										itemsTransferred++;
									}
									
									if (!sourceIt.MoveNext())
									{
										Log(Info,"All available convertible items from your armory were transfered into your inventory.");
										
										await ConvertInventoryIfRequested();
										
										return _done = true;
									}
								}
							}
						}
						
						if (itemsTransferred == 0)
						{
							Log(Error,"Some convertible items from your armory were not transfered into your inventory due to insufficient empty space.");
							
							return _done = true;
						}
					}
				}
			}
			else
			{
				await ConvertInventoryIfRequested();
			}

			return _done = true;
		}
	}
}
