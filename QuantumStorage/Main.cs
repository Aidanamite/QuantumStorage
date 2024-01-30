using HarmonyLib;
using SRML;
using SRML.Console;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using SRML.SR;
using SRML.SR.SaveSystem.Data;
using SRML.SR.SaveSystem;
using SRML.Utils.Enum;
using SRML.SR.Translation;
using UnityEngine.UI;
using SRML.SR.Patches;
using MonomiPark.SlimeRancher.Regions;
using SRML.SR.SaveSystem.Data.Ammo;
using System.Linq;
using SRML.Config.Attributes;

namespace QuantumStorage
{
	public class Main : ModEntryPoint
	{
		internal static Assembly modAssembly = Assembly.GetExecutingAssembly();
		internal static string modName = $"{modAssembly.GetName().Name}";
		internal static string modDir = $"{System.Environment.CurrentDirectory}\\SRML\\Mods\\{modName}";
		internal static StorageUI uiPrefab;
		internal static DroneUIProgramPicker uiPrefab2;
		internal static DroneUIProgramButton buttonPrefab;

		public override void PreLoad()
		{
			HarmonyInstance.PatchAll();
			var corralUIPrefab = Resources.FindObjectsOfTypeAll<CorralUI>().Find((x) => !x.name.EndsWith("(Clone)"));
			uiPrefab2 = Resources.FindObjectsOfTypeAll<DroneUIProgramPicker>().Find((x) => !x.name.EndsWith("(Clone)"));
			buttonPrefab = Resources.FindObjectsOfTypeAll<DroneUIProgramButton>().Find((x) => x.gameObject.name == "DroneUIProgramButton");
			var newPrefab = corralUIPrefab.gameObject.CreatePrefabCopy();
			Object.DestroyImmediate(newPrefab.GetComponent<CorralUI>());
			uiPrefab = newPrefab.AddComponent<StorageUI>();
			corralUIPrefab.CopyAllTo<BaseUI>(uiPrefab);
			foreach (UIMode value in System.Enum.GetValues(typeof(UIMode)))
			{
				TranslationPatcher.AddUITranslation($"t.{value.ToString().ToLower()}_items", $"{value} Items");
				TranslationPatcher.AddUITranslation(value.GetKey(), value.ToString());
				TranslationPatcher.AddUITranslation(value.GetKey(10), $"{value} x10");
			}
			TranslationPatcher.AddUITranslation("b.food", "Food");
			TranslationPatcher.AddUITranslation("b.misc", "Misc");
			TranslationPatcher.AddUITranslation("b.toys", "Toys");
			TranslationPatcher.AddPediaTranslation("no_desc", "No description available");
			TranslationPatcher.AddUITranslation("t.access_storage", "Access Quantum Storage");
			TranslationPatcher.AddUITranslation("drone.behaviour.quantum_silo", "Quantum Storage");
		}
		public override void Load()
		{
			var TeleporterDefinition = SRSingleton<GameContext>.Instance.LookupDirector.GetGadgetDefinition(Gadget.Id.TELEPORTER_GOLD);
			var AncientWaterPrefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPrefab(Identifiable.Id.MAGIC_WATER_LIQUID);
			var CorralPrefab = Resources.FindObjectsOfTypeAll<LandPlot>().Find((x) => x.name == "patchCorral");
			var NewGadgetPrefab = TeleporterDefinition.prefab.CreatePrefabCopy();
			var OldGadget = NewGadgetPrefab.GetComponent<Gadget>();
			var NewGadget = NewGadgetPrefab.AddComponent<StorageGadget>();
			Object.DestroyImmediate(NewGadgetPrefab.GetComponent<DisplayOnMap>());
			Object.DestroyImmediate(OldGadget);
			Object.DestroyImmediate(NewGadgetPrefab.transform.Find("Teleport Collider").gameObject);
			Object.DestroyImmediate(NewGadgetPrefab.transform.Find("DestLoc").gameObject);
			Object.DestroyImmediate(NewGadgetPrefab.transform.Find("Destination Icon").gameObject);
			Object.DestroyImmediate(NewGadgetPrefab.transform.Find("decoGlassScreen01").gameObject);
			NewGadgetPrefab.transform.Find("Teleport FX (1)").gameObject.SetActive(true);

			var Sphere = Object.Instantiate(AncientWaterPrefab.transform.Find("Sphere").gameObject, Vector3.up * 2, Quaternion.Euler(0, 0, 0), NewGadgetPrefab.transform);
			Sphere.name = "Sphere";
			AncientWaterPrefab.GetComponent<SphereCollider>().CopyAllTo(Sphere.AddComponent<SphereCollider>());
			Sphere.transform.localScale = Vector3.one * 1f;
			Sphere.AddComponent<StorageUIActivator>().uiPrefab = uiPrefab.gameObject;
			Sphere.AddComponent<StorageTrigger>().fxPrefab = Resources.FindObjectsOfTypeAll<GameObject>().Find((x) => x.name == "FX QuantumWarpOut");

			/*var Sphere2 = new GameObject("Trigger", typeof(StorageTrigger), typeof(SphereCollider)).transform;
			Sphere2.SetParent(Sphere.transform, false);
			Sphere2.localPosition = Vector3.zero;
			Sphere2.localRotation = Quaternion.Euler(0, 0, 0);
			Sphere2.name = "Trigger";
			var Sphere2Collider = Sphere2.GetComponent<SphereCollider>();
			AncientWaterPrefab.GetComponent<SphereCollider>().CopyAllTo(Sphere2Collider);
			Sphere2Collider.isTrigger = true;
			Sphere2.localScale = Vector3.one * 1.25f;
			Sphere2.GetComponent<StorageTrigger>().fxPrefab = Resources.FindObjectsOfTypeAll<GameObject>().Find((x) => x.name == "FX QuantumWarpOut");*/

			NewGadget.id = Id.QUANTUM_SILO;
			GadgetRegistry.RegisterBlueprintLock(Id.QUANTUM_SILO, CreateQuantumSiloLocker);
			var NewGadgetDefinition = ScriptableObject.CreateInstance<GadgetDefinition>();
			NewGadgetDefinition.blueprintCost = 20000;
			NewGadgetDefinition.buyCountLimit = -1;
			NewGadgetDefinition.buyInPairs = false;
			NewGadgetDefinition.countLimit = -1;
			NewGadgetDefinition.countOtherIds = new Gadget.Id[0];
			NewGadgetDefinition.craftCosts = new GadgetDefinition.CraftCost[] {
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.GOLD_PLORT, amount = 5 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.QUANTUM_PLORT, amount = 48 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.MANIFOLD_CUBE_CRAFT, amount = 18 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.STRANGE_DIAMOND_CRAFT, amount = 5 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.SPIRAL_STEAM_CRAFT, amount = 9 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.DEEP_BRINE_CRAFT, amount = 9 },
				new GadgetDefinition.CraftCost() { id = Identifiable.Id.GLASS_SHARD_CRAFT, amount = 24 }
			}; ;
			NewGadgetDefinition.destroyOnRemoval = false;
			NewGadgetDefinition.icon = TeleporterDefinition.icon;
			NewGadgetDefinition.id = Id.QUANTUM_SILO;
			NewGadgetDefinition.pediaLink = PediaDirector.Id.WARP_TECH;
			NewGadgetDefinition.prefab = NewGadgetPrefab;
			StorageUI.titleIcon = TeleporterDefinition.icon;
			LookupRegistry.RegisterGadget(NewGadgetDefinition);
			Id.QUANTUM_SILO.GetTranslation().SetNameTranslation("Quantum Storage").SetDescriptionTranslation("A little hole into a pocket dimension that can be used for storing items");
			foreach (var droneMetadata in DroneRegistry.GetMetadatas())
			{
				droneMetadata.destinations = droneMetadata.destinations.AddToArray(new DroneMetadata.Program.Behaviour() { id = "drone.behaviour.quantum_silo", image = NewGadgetDefinition.icon, isCompatible = (x) => true, types = new System.Type[] { typeof(QuantumSiloDestination) } });
				droneMetadata.sources = droneMetadata.sources.AddToArray(new DroneMetadata.Program.Behaviour() { id = "drone.behaviour.quantum_silo", image = NewGadgetDefinition.icon, isCompatible = (x) => true, types = new System.Type[] { typeof(QuantumSiloSource) } });
			}
			SRCallbacks.OnMainMenuLoaded += (x) => StorageUI.slots.Clear();
		}
		public override void PostLoad()
		{
			SaveRegistry.RegisterWorldDataLoadDelegate(ReadData);
			SaveRegistry.RegisterWorldDataSaveDelegate(WriteData);
		}
		public static void Log(object message) => Console.Log($"[{modName}]: " + message);
		public static void LogError(object message) => Console.LogError($"[{modName}]: " + message);
		public static void LogWarning(object message) => Console.LogWarning($"[{modName}]: " + message);
		public static void LogSuccess(object message) => Console.LogSuccess($"[{modName}]: " + message);
		public static Texture2D LoadImage(string filename, int width, int height)
		{
			var spriteData = modAssembly.GetManifestResourceStream(modName + "." + filename);
			var rawData = new byte[spriteData.Length];
			spriteData.Read(rawData, 0, rawData.Length);
			var tex = new Texture2D(width, height);
			tex.LoadImage(rawData);
			return tex;
		}

		public static void WriteData(CompoundDataPiece data)
		{
			CompoundDataPiece storage;
			if (data.HasPiece("storage"))
			{
				storage = data.GetCompoundPiece("storage");
				storage.DataList.Clear();
			}
			else
			{
				storage = new CompoundDataPiece("storage");
				data.AddPiece(storage);
			}
			var i = 0;
			foreach (var p in StorageUI.slots)
				storage.AddPiece(p.ToData("slot_" + i++));
			storage.SetValue("count", i);
		}
		public static void ReadData(CompoundDataPiece data)
		{
			StorageUI.slots.Clear();
			if (!data.HasPiece("storage"))
				return;
			var storage = data.GetCompoundPiece("storage");
			var c = storage.GetValue<int>("count");
			for (int i = 0; i < c; i++)
			{
				var slot = storage.GetStorageAmmo("slot_" + i);
				if (slot.IsEmpty())
					continue;
				var oSlot = StorageUI.slots.Find((x) => x.Slot.id == slot.Slot.id);
				if (oSlot == null)
					StorageUI.slots.Add(slot);
				else
					oSlot.Add(slot);

			}
		}

		public static GameObject CreateSelectionUI(string titleKey, Sprite titleIcon, List<ModeOption> options)
		{
			var ui = Object.Instantiate(uiPrefab2);
			ui.title.text = GameContext.Instance.MessageDirector.Get("ui", titleKey);
			ui.icon.sprite = titleIcon;
			List<Button> buttons = new List<Button>();
			foreach (var option in options)
			{
				var button = Object.Instantiate(buttonPrefab, ui.contentGrid).Init(option, null);
				button.button.onClick.AddListener(() => {
					ui.Close();
					option.Selected();
				});
				buttons.Add(button.button);
				if (buttons.Count == 1)
					button.button.gameObject.AddComponent<InitSelected>();
			}
			int num = Mathf.CeilToInt(buttons.Count / 6f);
			for (int j = 0; j < buttons.Count; j++)
			{
				int y = j / 6;
				int x = j % 6;
				Navigation navigation = buttons[j].navigation;
				navigation.mode = Navigation.Mode.Explicit;
				if (y > 0)
					navigation.selectOnUp = buttons[(y - 1) * 6 + x];
				if (y < num - 1)
					navigation.selectOnDown = buttons[Mathf.Min((y + 1) * 6 + x, buttons.Count - 1)];
				if (x > 0)
					navigation.selectOnLeft = buttons[y * 6 + (x - 1)];
				if (x < 5 && j < buttons.Count - 1)
					navigation.selectOnRight = buttons[y * 6 + (x + 1)];
				buttons[j].navigation = navigation;
			}
			return ui.gameObject;
		}

		public static GadgetDirector.BlueprintLocker CreateQuantumSiloLocker(GadgetDirector director)
		{
			var ShouldUnlock = (System.Func<Gadget.Id[], ProgressDirector.ProgressType[], bool>)AccessTools.Method(typeof(GadgetDirector), "ShouldUnlock").CreateDelegate(typeof(System.Func<Gadget.Id[], ProgressDirector.ProgressType[], bool>), director);
			return new GadgetDirector.BlueprintLocker(director, Id.QUANTUM_SILO, () =>
				Config.EasyUnlock
					? ShouldUnlock(new[] { Gadget.Id.TELEPORTER_PINK, Gadget.Id.WARP_DEPOT_PINK }, new[] { ProgressDirector.ProgressType.UNLOCK_VIKTOR_MISSIONS })
					: ShouldUnlock(new[] { Gadget.Id.TELEPORTER_GOLD, Gadget.Id.WARP_DEPOT_GOLD }, new ProgressDirector.ProgressType[0])
				, 0);

		}
	}

	[ConfigFile("settings")]
	public static class Config
	{
		public static bool EasyUnlock = false;
	}

	public class ModeOption : DroneMetadata.Program.BaseComponent
	{
		System.Action onClick;
		public ModeOption(Sprite Icon, string NameKey, System.Action OnSelect)
		{
			id = NameKey;
			image = Icon;
			onClick = OnSelect;
		}
		public void Selected() => onClick?.Invoke();
	}

	class QuantumSiloDestination : DroneProgramDestination<StorageGadget>
	{
		protected override IEnumerable<Orientation> GetTargetOrientations() => new Orientation[] { new Orientation(destination.transform.position + destination.transform.forward * 3 + Vector3.up * 2, Quaternion.LookRotation(-destination.transform.forward)) };
		protected override Vector3 GetTargetPosition() => destination.transform.position + destination.transform.forward * 3 + Vector3.up * 2;
		protected override bool OnAction_Deposit(bool overflow)
		{
			foreach (var slot in drone.ammo.Slots)
				drone.ammo.DepositAmmo(slot, slot.count);
			return true;
		}
		public override FastForward_Response FastForward(Identifiable.Id id, bool overflow, double endTime, int maxFastForward)
		{
			StorageUI.AddItems(id, maxFastForward);
			return new FastForward_Response() { deposits = maxFastForward };
		}
		public override int GetAvailableSpace(Identifiable.Id id) => StorageGadget.All.Any((x) => x.Network && x.Network == drone.network) ? int.MaxValue - StorageUI.GetCount(id) : 0;
		protected override IEnumerable<StorageGadget> Prioritize(IEnumerable<StorageGadget> destinations) => destinations;
		protected override IEnumerable<StorageGadget> GetDestinations(Identifiable.Id id, bool overflow) => StorageGadget.All.FindAll((x) => x.Network && x.Network == drone.network);
	}

	class QuantumSiloSource : DroneProgramSource<StorageGadget.Target>
	{
		protected override IEnumerable<Orientation> GetTargetOrientations(StorageGadget.Target source) => new Orientation[] { new Orientation(source.Storage.transform.position + source.Storage.transform.forward * 3 + Vector3.up * 2, Quaternion.LookRotation(-source.Storage.transform.forward)) };
		protected override Vector3 GetTargetPosition(StorageGadget.Target source) => source.Storage.transform.position + source.Storage.transform.forward * 3 + Vector3.up * 2;
		protected override GameObject GetTargetGameObject(StorageGadget.Target source) => source.Storage.gameObject;
		public override IEnumerable<DroneFastForwarder.GatherGroup> GetFastForwardGroups(double endTime)
		{
			var t = new List<DroneFastForwarder.GatherGroup>();
			foreach (var slot in StorageUI.slots.FindAll((x) => predicate(x.Slot.id)))
				t.Add(new QuantumSiloGatherGroup(slot.Slot.id));
			return t;
		}
		protected override bool OnAction()
		{
			var slot = StorageUI.slots.Find((x) => x.Slot.id == source.Id);
			if (slot != null)
				drone.ammo.TryAddAmmo(slot, slot.Slot.count);
			return true;
		}
		protected override IEnumerable<StorageGadget.Target> GetSources(System.Predicate<Identifiable.Id> predicate)
		{
			var slot = StorageUI.slots.Find(x => predicate(x.Slot.id));
			if (slot == null)
				return new List<StorageGadget.Target>();
			return StorageGadget.All.FindAll((x) => x.Network && x.Network == drone.network).ConvertAll(x => new StorageGadget.Target(x, slot.Slot.id));
		}
	}

	class QuantumSiloGatherGroup : DroneFastForwarder.GatherGroup
	{
		Identifiable.Id target;
		public QuantumSiloGatherGroup(Identifiable.Id Target) => target = Target;
		public override void Decrement(int decrement) => StorageUI.TakeItems(id, decrement);
		public override int count => StorageUI.GetCount(id);
		public override void Dispose() { }
		public override bool overflow => true;
		public override Identifiable.Id id => target;
	}

	[EnumHolder]
	static class Id
	{
		[GadgetCategorization(GadgetCategorization.Rule.MISC)]
		public static readonly Gadget.Id QUANTUM_SILO;
	}

	public static class ExtentionMethods
	{
		public static bool Exists<T>(this T[] t, System.Predicate<T> predicate)
		{
			foreach (var i in t)
				if (predicate(i))
					return true;
			return false;
		}
		public static T Find<T>(this T[] t, System.Predicate<T> predicate)
		{
			foreach (var i in t)
				if (predicate(i))
					return i;
			return default(T);
		}
		public static int IndexOf<T>(this T[] t, System.Predicate<T> predicate)
		{
			for (int i = 0; i < t.Length; i++)
				if (predicate(t[i]))
					return i;
			return -1;
		}
		public static int IndexOf<T>(this T[] t, System.Func<T, int, bool> predicate)
		{
			for (int i = 0; i < t.Length; i++)
				if (predicate(t[i], i))
					return i;
			return -1;
		}
		public static bool TryAddAmmo(this Ammo ammo, StorageSlot slot, int amt = 1)
		{
			if (slot.IsEmpty())
				return false;
			if (!ammo.CouldAddToSlot(slot.Slot.id))
				return false;
			var ammoPersist = PersistentAmmoManager.GetPersistentAmmoForAmmo(ammo.ammoModel);
			var ind = ammo.GetAmmoIdx(slot.Slot.id);
			amt = Mathf.Min(amt, slot.Slot.count);
			if (ind == null)
			{
				ind = ammo.Slots.IndexOf((x, y) => x.IsEmpty() && (ammo.GetSlotPredicate(y) == null ? true : ammo.GetSlotPredicate(y)(slot.Slot.id)));
				amt = Mathf.Min(ammo.GetSlotMaxCount(slot.Slot.id, ind.Value), amt);
				var nSlot = slot.Take(amt);
				ammo.Slots[ind.Value] = nSlot;
				ammoPersist.DataModel.slots[ind.Value] = nSlot;
			} else
			{
				amt = Mathf.Min(ammo.GetSlotMaxCount(slot.Slot.id, ind.Value) - ammo.Slots[ind.Value].count, amt);
				slot.Take(amt).AddTo(ammo.Slots[ind.Value], ammoPersist.DataModel.slots[ind.Value]);
			}
			if (slot.IsEmpty())
				StorageUI.slots.Remove(slot);
			return true;
		}
		public static void DepositAmmo(this Ammo ammo, Ammo.Slot slot, int amt = 1)
		{
			if (slot.IsEmpty() || amt <= 0)
				return;
			var ammoPersist = PersistentAmmoManager.GetPersistentAmmoForAmmo(ammo.ammoModel);
			var ind = ammo.Slots.IndexOf((x) => x == slot);
			var persistSlot = ind == -1 || ind >= ammoPersist.DataModel.slots.Length ? null : ammoPersist.DataModel.slots[ind];
			StorageUI.AddItems(new StorageSlot(slot, persistSlot).Take(amt));
			if (slot.IsEmpty())
				ammo.Slots[ind] = null;
		}
		public static bool IsEmpty(this Ammo.Slot slot) => slot == null || slot.id == Identifiable.Id.NONE || slot.count <= 0;
		public static bool IsEmpty(this StorageSlot slot) => slot == null || slot.Slot.IsEmpty();
		public static Ammo GetCurrentAmmo(this PlayerState player) => player.GetAmmo(player.GetAmmoMode());
		public static SlimeEmotionData Clone(this SlimeEmotionData data)
		{
			if (data == null)
				return null;
			var n = new SlimeEmotionData();
			foreach (var p in data)
				n[p.Key] = p.Value;
			return n;
		}

		public static void AverageIn(this Ammo.Slot data, SlimeEmotionData emotions, int amt = 1)
		{
			if (emotions == null)
				return;
			if (data.emotions == null)
			{
				data.emotions = emotions.Clone();
				return;
			}
			float weight = amt / data.count;
			float num = 1f - weight;
			foreach (var emotion in emotions)
				data.emotions[emotion.Key] = data.emotions[emotion.Key] * num + emotion.Value * weight;
		}
		public static Sprite CreateSprite(this Texture2D texture) => Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1);
		public static CompoundDataPiece ToData(this Ammo.Slot slot, string name)
		{
			var data = new CompoundDataPiece(name);
			data.SetValue("id", slot.id);
			data.SetValue("count", slot.count);
			if (slot.emotions != null)
			{
				var emotionData = new CompoundDataPiece("emotions");
				var i = 0;
				foreach (var p in slot.emotions)
				{
					var j = i++;
					emotionData.SetValue("key_" + j, p.Key);
					emotionData.SetValue("value_" + j, p.Value);
				}
				emotionData.SetValue("count", i);
				data.AddPiece(emotionData);
			}
			return data;
		}
		public static CompoundDataPiece ToData(this StorageSlot slot, string name)
		{
			var data = new CompoundDataPiece(name);
			data.AddPiece(slot.Slot.ToData("slot"));
			var persistent = new CompoundDataPiece("persistent");
			foreach (var p in slot.Persistent.data)
				if (p == null)
					persistent.AddPiece(new CompoundDataPiece(""));
				else
					persistent.AddPiece(p);
			data.AddPiece(persistent);
			return data;
		}
		public static Ammo.Slot GetAmmo(this CompoundDataPiece data, string key)
		{
			if (!data.HasPiece(key))
				return null;
			var ammoData = data.GetCompoundPiece(key);
			if (ammoData == null)
				return null;
			SlimeEmotionData emotions = null;
			if (ammoData.HasPiece("emotions"))
			{
				var emotionData = ammoData.GetCompoundPiece("emotions");
				emotions = new SlimeEmotionData();
				var c = emotionData.GetValue<int>("count");
				for (int i = 0; i < c; i++)
					emotions.Add(emotionData.GetValue<SlimeEmotions.Emotion>("key_" + i), emotionData.GetValue<float>("value_" + i));
			}
			return new Ammo.Slot(ammoData.GetValue<Identifiable.Id>("id"), ammoData.GetValue<int>("count")) { emotions = emotions };
		}
		public static StorageSlot GetStorageAmmo(this CompoundDataPiece data, string key)
		{
			if (!data.HasPiece(key))
				return null;
			var storeData = data.GetCompoundPiece(key);
			if (storeData == null)
				return null;
			var slot = storeData.GetAmmo("slot");
			if (slot == null)
			{
				slot = data.GetAmmo(key);
				if (slot.IsEmpty())
					return null;
				return new StorageSlot(slot);
			}
			var persistent = new PersistentAmmoSlot() { data = new List<CompoundDataPiece>() };
			if (storeData.HasPiece("persistent"))
				foreach (CompoundDataPiece p in storeData.GetCompoundPiece("persistent").DataList)
					persistent.data.Add(p);
			return new StorageSlot(slot, persistent);
		}
		public static string GetKey(this UIMode mode, int amount = 1) => "b." + mode.ToString().ToLower() + (amount == 1 ? "" : $"_{amount}");
		public static bool Eject(this StorageSlot slot, System.Func<Vector3> spawnPoint, RegionRegistry.RegionSetId setId, int count)
		{
			if (slot.IsEmpty())
				return false;
			var prefab = GameContext.Instance.LookupDirector.GetPrefab(slot.Slot.id);
			if (!prefab)
				return false;
			while (!slot.IsEmpty() && count > 0)
			{

				var obj = ExtendedData.InstantiateActorWithData(prefab, setId, spawnPoint(), default, slot.Persistent.PopTop());
				var scale = obj.transform.localScale;
				obj.transform.localScale = Vector3.one * 0.01f;
				obj.GetComponent<Identifiable>().StartCoroutine(obj.transform.ScaleTo(scale, 0.5f));
				slot.Slot.count--;
				count--;
			}
			return true;
		}
		public static IEnumerator ScaleTo(this Transform t, Vector3 target, float time)
		{
			var start = t.localScale;
			var passed = 0f;
			while (passed < time)
			{
				passed += Time.deltaTime;
				t.localScale = Vector3.Lerp(start, target, passed / time);
				yield return null;
			}
			yield break;
		}

		public static float[] ToArray(this Vector3 value) => new float[] { value.x, value.y, value.z };

		public static bool CanPlayerHold(this Identifiable.Id id, bool checkSize = true) {
			if (id == Identifiable.Id.NONE)
				return false;
			if (checkSize && GameContext.Instance.LookupDirector.GetPrefab(id).GetComponent<Vacuumable>().size != Vacuumable.Size.NORMAL)
				return false;
			var ammo = SceneContext.Instance.PlayerState.Ammo;
			for (int i = 0; i < ammo.GetUsableSlotCount(); i++)
				if (ammo.GetSlotPredicate(i)(id))
					return true;
			return false;
		}

		public static Ammo.Slot Clone(this Ammo.Slot slot) => new Ammo.Slot(slot.id, slot.count) { emotions = slot.emotions.Clone() };
	}

	public class StorageGadget : Gadget
	{
		static List<StorageGadget> all = new List<StorageGadget>();
		public static List<StorageGadget> All => new List<StorageGadget>(all);
		public DroneNetwork Network { get; private set; }
		new void Awake()
		{
			base.Awake();
			all.Add(this);
			Network = DroneNetwork.Find(gameObject);
		}
		void OnDestroy() => all.Remove(this);

		public class Target
        {
			public StorageGadget Storage;
			public Identifiable.Id Id;
			public Target(StorageGadget storage, Identifiable.Id id)
            {
				Storage = storage;
				Id = id;
            }
        }
	}

	public class StorageUIActivator : UIActivator
	{
		public override GameObject Activate()
		{
			var G = Main.CreateSelectionUI("t.access_storage", StorageUI.titleIcon, new List<ModeOption>
			{
				new ModeOption(SceneContext.Instance.PediaDirector.Get(PediaDirector.Id.WARP_TECH).icon,UIMode.Withdraw.GetKey(),() => {
					var g = Instantiate(Main.uiPrefab);
					g.gameObject.SetActive(true);
					if (g)
						g.RebuildUI(UIMode.Withdraw, () => Activate());
				}),
				new ModeOption(SceneContext.Instance.PediaDirector.Get(PediaDirector.Id.SILO).icon,UIMode.Deposit.GetKey(),() => {
					var g = Instantiate(Main.uiPrefab);
					g.gameObject.SetActive(true);
					if (g)
						g.RebuildUI(UIMode.Deposit, () => Activate());
				}),
				new ModeOption(GameContext.Instance.LookupDirector.GetGadgetDefinition(Gadget.Id.DRONE).prefab.GetComponent<DroneGadget>().metadata.imageSourceFreeRange,UIMode.Eject.GetKey(),() => {
					var g = Instantiate(Main.uiPrefab);
					g.gameObject.SetActive(true);
					if (g) {
						var bounds = GetComponent<Collider>().bounds;
						g.ejectPoint = () => transform.TransformPoint(RandomAngle() * (Vector3.up * Mathf.Max(bounds.extents.ToArray())));
						g.ejectRegion = GetComponentInParent<Region>().setId;
						g.RebuildUI(UIMode.Eject, () => Activate());
					}
				})
			});
			return G;
		}
		static Quaternion RandomAngle() => Quaternion.AngleAxis(Random.Range(0f, 360), new Vector3(Random.Range(-1f, 1), Random.Range(-1f, 1), Random.Range(-1f, 1)).normalized);
	}

	public class StorageTrigger : SRBehaviour
	{
		public GameObject fxPrefab;
		static List<Vacuumable> collected = new List<Vacuumable>();
		void OnTriggerEnter(Collider collider)
		{
			if (collider.isTrigger)
				return;
			var vac = collider.GetComponentInParent<Vacuumable>();
			TryCollect(vac);
		}
		public void TryCollect(Vacuumable vac)
		{
			if (!vac || collected.Contains(vac))
				return;
			if (!SceneContext.Instance.GameModel.AllActors().ContainsKey(Identifiable.GetActorId(vac.gameObject)))
				return;
			var id = Identifiable.GetId(vac.gameObject);
			if (id != Identifiable.Id.NONE && vac.isLaunched())
			{
				StorageUI.AddItem(vac.GetComponent<Identifiable>());
				if (fxPrefab)
					SpawnAndPlayFX(fxPrefab, vac.transform.position, vac.transform.rotation);
				collected.RemoveAll((x) => !x);
				collected.Add(vac);
				SceneContext.Instance.GameModel.DestroyActorModel(vac.gameObject);
				vac.StartCoroutine(ExecuteNextFrame(() => Destroyer.DestroyActor(vac.gameObject, "QuantumStorageTrigger:TryCollect", true)));
			}
		}
		IEnumerator ExecuteNextFrame(System.Action action)
		{
			yield return null;
			action();
			yield break;
		}
	}

	public class StorageSlot
	{
		public Ammo.Slot Slot;
		public PersistentAmmoSlot Persistent;
		public StorageSlot(Ammo.Slot slot, PersistentAmmoSlot persistent = null)
		{
			Slot = slot;
			if (persistent == null)
				persistent = new PersistentAmmoSlot() { data = new List<CompoundDataPiece>() };
			while (persistent.data.Count < Slot.count)
				persistent.data.Add(new CompoundDataPiece(""));
			Persistent = persistent;
		}
		public void Add(StorageSlot slot) => Add(slot.Slot, slot.Persistent);
		public void Add(Ammo.Slot slot, PersistentAmmoSlot persistent)
		{
			Slot.count += slot.count;
			Slot.AverageIn(slot.emotions, slot.count);
			if (persistent != null)
			{
				int i = slot.count;
				foreach (var p in persistent.data)
					if (i-- < 0)
						break;
					else
						Persistent.data.Add(p);
			}
			while (Persistent.data.Count < Slot.count)
				Persistent.data.Add(new CompoundDataPiece(""));
		}
		public void AddTo(Ammo.Slot slot, PersistentAmmoSlot persistent) => new StorageSlot(slot, persistent).Add(this);
		public StorageSlot Take(int count)
		{
			if (count > Slot.count)
				count = Slot.count;
			var nSlot = new Ammo.Slot(Slot.id, count) { emotions = Slot.emotions.Clone() };
			var nPersistent = new PersistentAmmoSlot() { data = Persistent.data.GetRange(0, count) };
			Slot.count -= count;
			Persistent.data.RemoveRange(0, count);
			return new StorageSlot(nSlot, nPersistent);
		}
		public static implicit operator Ammo.Slot(StorageSlot slot) => slot.Slot;
		public static implicit operator PersistentAmmoSlot(StorageSlot slot) => slot.Persistent;
	}

	public class StorageUI : BaseUI
	{
		PurchaseUI ui;
		UIMode mode;
		public static List<StorageSlot> slots = new List<StorageSlot>();
		public static Sprite titleIcon;
		public System.Func<Vector3> ejectPoint;
		public RegionRegistry.RegionSetId ejectRegion;
		int amount => Input.GetKey(KeyCode.LeftShift) ? 10 : 1;
		public void RebuildUI(UIMode Mode, System.Action onClose = null)
		{
			if (ui)
				Destroy(ui.gameObject);
			mode = Mode;
			GameObject gameObject = Mode == UIMode.Deposit ? CreateDepositUI(onClose) : CreateWithdrawUI(Mode == UIMode.Eject, onClose);
			if (loop != null)
				StopCoroutine(loop);
			loop = StartCoroutine(UpdateLoop(onClose));
			//enabled = true;
			ui = gameObject.GetComponent<PurchaseUI>();
			statusArea = ui.statusArea;
		}

		protected GameObject CreateWithdrawUI(bool eject, System.Action onClose = null)
		{
			List<PurchaseUI.Purchasable> list = new List<PurchaseUI.Purchasable>();
			Dictionary<string, List<PurchaseUI.Purchasable>> dictionary = new Dictionary<string, List<PurchaseUI.Purchasable>>();
			foreach (var s in slots)
			{
				if (!eject && !s.Slot.id.CanPlayerHold())
					continue;
				var data = GetIdentifiableData(s.Slot.id);
				var p = new PurchaseUI.Purchasable(
					data.NameKey, data.Icon, data.Icon, data.DescKey, 0, data.PediaId, () => {
						if (eject ? s.Eject(ejectPoint, ejectRegion, amount) : SceneContext.Instance.PlayerState.GetCurrentAmmo().TryAddAmmo(s, amount))
						{
							if (s.IsEmpty())
								slots.Remove(s);
							ui.Rebuild(false);
						}
					}, () => !s.IsEmpty(), () => !s.IsEmpty(), null, null, () => s.Slot.count
				);
				list.Add(p);
				if (dictionary.ContainsKey(data.TabKey))
					dictionary[data.TabKey].Add(p);
				else
					dictionary.Add(data.TabKey, new List<PurchaseUI.Purchasable> { p });
			}
			GameObject gameObject = GameContext.Instance.UITemplates.CreatePurchaseUI(titleIcon, MessageUtil.Qualify("ui", eject ? $"t.{UIMode.Eject.ToString().ToLower()}_items" : $"t.{UIMode.Withdraw.ToString().ToLower()}_items"), list.ToArray(), true, () => { onClose?.Invoke(); Close(); }, false);
			List<PurchaseUI.Category> categories = new List<PurchaseUI.Category>();
			foreach (var p in dictionary)
			{
				p.Value.Sort((a, b) => string.Compare(a.nameKey, b.nameKey));
				categories.Add(new PurchaseUI.Category(p.Key, p.Value.ToArray()));
			}
			categories.Sort((a, b) => string.Compare(a.name, b.name));
			gameObject.GetComponent<PurchaseUI>().SetCategories(categories);
			gameObject.GetComponent<PurchaseUI>().SetPurchaseMsgs(eject ? UIMode.Eject.GetKey(amount) : UIMode.Withdraw.GetKey(amount), eject ? UIMode.Eject.GetKey() : UIMode.Withdraw.GetKey());
			return gameObject;
		}


		protected GameObject CreateDepositUI(System.Action onClose = null)
		{
			List<PurchaseUI.Purchasable> list = new List<PurchaseUI.Purchasable>();
			var ammo = SceneContext.Instance.PlayerState.GetCurrentAmmo();
			foreach (var s in ammo.Slots)
			{
				if (s.IsEmpty())
					continue;
				var data = GetIdentifiableData(s.id);
				list.Add(new PurchaseUI.Purchasable(
					data.NameKey, data.Icon, data.Icon, data.DescKey, 0, data.PediaId, () =>
					{
						ammo.DepositAmmo(s, amount);
						ui.Rebuild(false);
					}, () => !s.IsEmpty(), () => !s.IsEmpty(), null, null, () => GetCount(s.id)
				));
			}
			GameObject gameObject = GameContext.Instance.UITemplates.CreatePurchaseUI(titleIcon, MessageUtil.Qualify("ui", $"t.{UIMode.Deposit.ToString().ToLower()}_items"), list.ToArray(), true, () => { onClose?.Invoke(); Close(); }, false);
			gameObject.GetComponent<PurchaseUI>().SetPurchaseMsgs(UIMode.Deposit.GetKey(amount), UIMode.Deposit.GetKey());
			return gameObject;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			if (ui)
				Destroy(ui.gameObject);
		}

		Coroutine loop;
		IEnumerator UpdateLoop(System.Action onClose)
		{
			yield return new WaitForEndOfFrame();
			while (true) {
				if (ui && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.LeftShift)))
				{
					ui.SetPurchaseMsgs(mode.GetKey(amount), mode.GetKey());
					ui.Rebuild(false);
				}
				yield return new WaitForEndOfFrame();
			}
		}

		public static int GetCount(Identifiable.Id id)
		{
			var i = slots.Find((x) => x.Slot.id == id);
			return i == null ? 0 : i.Slot.count;
		}
		static List<IdentifiableData> dataCache = new List<IdentifiableData>();
		public static IdentifiableData GetIdentifiableData(Identifiable.Id Id)
		{
			var data = dataCache.Find((x) => x.Id == Id);
			if (data != null)
				return data;
			data = new IdentifiableData();
			data.PediaId = SceneContext.Instance.PediaDirector.GetPediaId(Id);
			var PIdStr = data.PediaId == null ? null : data.PediaId.Value.ToString().ToLowerInvariant();
			var IIdStr = Id.ToString().ToLowerInvariant();
			var flag = Identifiable.IsToy(Id);
			if (flag)
			{
				try
				{
					var toy = GameContext.Instance.LookupDirector.GetToyDefinition(Id);
					data.Icon = toy.Icon;
					data.NameKey = "m.toy.name." + toy.NameKey;
					data.DescKey = "m.toy.desc." + toy.NameKey;
				} catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				data.Icon = GameContext.Instance.LookupDirector.GetIcon(Id);
				if (GameContext.Instance.MessageDirector.Get("actor", "l." + IIdStr) != null)
					data.NameKey = MessageUtil.Qualify("actor", "l." + IIdStr);
				else if (GameContext.Instance.MessageDirector.Get("pedia", "t." + IIdStr) != null)
					data.NameKey = MessageUtil.Qualify("pedia", "t." + IIdStr);
				else if (data.PediaId != null && GameContext.Instance.MessageDirector.Get("pedia", "t." + PIdStr) != null)
					data.NameKey = MessageUtil.Qualify("pedia", "t." + PIdStr);
				if (PIdStr != null)
					data.DescKey = "m.intro." + PIdStr;
				else
					data.DescKey = "no_desc";
			}
			if (Identifiable.IsSlime(Id))
				data.TabKey = "slimes";
			else if (Identifiable.IsFood(Id) || Identifiable.IsChick(Id))
				data.TabKey = "food";
			else if (Identifiable.IsEcho(Id) || Identifiable.IsEchoNote(Id) || Identifiable.IsOrnament(Id))
				data.TabKey = "decorations";
			else if (Identifiable.IsCraft(Id) || Identifiable.IsPlort(Id))
				data.TabKey = "resources";
			else if (Identifiable.IsToy(Id))
				data.TabKey = "toys";
			else
				data.TabKey = "misc";
			dataCache.Add(data);
			return data;
		}

		public static void AddItem(Identifiable identifiable)
		{
			var slot = new Ammo.Slot(identifiable.id, 1) {
				emotions = identifiable.GetComponent<SlimeEmotions>() ? new SlimeEmotionData(identifiable.GetComponent<SlimeEmotions>()) : null
			};
			var persistent = new PersistentAmmoSlot() { data = new List<CompoundDataPiece>() { ExtendedData.ReadDataFromGameObject(identifiable.gameObject) } };
			AddItems(slot, persistent);
		}

		public static void AddItems(StorageSlot slot) => AddItems(slot, slot);
		public static void AddItems(Ammo.Slot slot, PersistentAmmoSlot persistent = null)
		{
			if (slot.IsEmpty())
				return;
			var store = slots.FirstOrDefault((x) => x.Slot.id == slot.id);
			if (store == null)
				slots.Add(new StorageSlot(slot, persistent));
			else
				store.Add(slot, persistent);
		}
		public static void AddItems(Identifiable.Id id, int count) => AddItems(new Ammo.Slot(id, count));

		public static StorageSlot TakeItems(Identifiable.Id id, int count)
		{
			var store = slots.FirstOrDefault((x) => x.Slot.id == id);
			if (store != null)
			{
				var items = store.Take(count);
				if (store.IsEmpty())
					slots.Remove(store);
				return items;
			}
			return null;
		}
	}

	public enum UIMode
	{
		Withdraw, Deposit, Eject
	}

	public class IdentifiableData
	{
		public Identifiable.Id Id; public string NameKey; public string DescKey; public Sprite Icon; public PediaDirector.Id? PediaId; public string TabKey;
	}

	[HarmonyPatch(typeof(Vacuumable), nameof(Vacuumable.ProcessCollisionEnter))]
	class Patch_VacuumableCollision
	{
		static void Prefix(Vacuumable __instance, Collision col)
		{
			StorageTrigger s = col.collider.GetComponent<StorageTrigger>();
			if (s)
				s.TryCollect(__instance);
		}
	}
}