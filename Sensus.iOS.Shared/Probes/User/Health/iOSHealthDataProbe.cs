using Foundation;
using HealthKit;
using MetalPerformanceShaders;
using Newtonsoft.Json;
using Sensus.Probes.User.Health;
using Sensus.UI.UiProperties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sensus.iOS.Probes.User.Health
{
	public class iOSHealthDataProbe : HealthDataProbe
	{
		private readonly HKHealthStore _healthStore;

		public iOSHealthDataProbe()
		{
			_healthStore = new HKHealthStore();

			Collectors = new List<HealthDataCollector>();
		}

		#region Health data collector classes...
		public abstract class HealthDataCollector
		{
			public HealthDataCollector()
			{

			}

			public HealthDataCollector(HKHealthStore healthStore)
			{
				HealthStore = healthStore;
			}

			[JsonIgnore]
			public HKHealthStore HealthStore { get; set; }
			[JsonIgnore]
			public HKObjectType ObjectType { get; protected set; }
			[JsonIgnore]
			public string Key { get; protected set; }

			public virtual Task InitializeAsync()
			{
				return Task.CompletedTask;
			}

			public abstract Task<List<HealthDatum>> GetDataAsync();
		}

		public class SampleCollector : HealthDataCollector
		{
			private HKQuantityTypeIdentifier _sampleType;
			private HKUnit _unit;

			public HKQuantityTypeIdentifier SampleType
			{
				get
				{
					return _sampleType;
				}	
				set
				{
					_sampleType = value;

					ObjectType = HKQuantityType.Create(value);
					Key = value.ToString();
				}
			}
			public uint QueryAnchor { get; set; }

			public string Unit
			{
				get
				{
					return _unit.UnitString;
				}
				set
				{
					_unit = HKUnit.FromString(value);
				}
			}

			public SampleCollector()
			{

			}

			public SampleCollector(HKHealthStore healthStore, HKQuantityTypeIdentifier sampleType, HKUnit unit) : base(healthStore)
			{
				SampleType = sampleType;
				_unit = unit;
			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				TaskCompletionSource<List<HealthDatum>> completionSource = new TaskCompletionSource<List<HealthDatum>>();

				HealthStore.ExecuteQuery(new HKAnchoredObjectQuery(ObjectType as HKSampleType, null, (nuint)QueryAnchor, nuint.MaxValue, new HKAnchoredObjectResultHandler2(
					(query, samples, newQueryAnchor, error) =>
					{
						List<HealthDatum> data = new List<HealthDatum>();

						try
						{
							if (error == null)
							{
								if (QueryAnchor == 0 && samples.Length > 1)
								{
									samples = new[] { samples.Last() };
								}

								foreach (HKQuantitySample sample in samples)
								{
									HealthDatum datum = new HealthDatum(SampleType.ToString(), sample.Quantity.GetDoubleValue(_unit).ToString(), Unit, sample.Source.ToString(), (DateTime)sample.StartDate, (DateTime)sample.EndDate, DateTimeOffset.UtcNow);

									data.Add(datum);
								}

								QueryAnchor = (uint)newQueryAnchor;
							}
							else
							{
								SensusServiceHelper.Get().Logger.Log($"Failed to collect {SampleType}: " + error.Description, LoggingLevel.Normal, GetType());
							}
						}
						catch (Exception ex)
						{
							SensusServiceHelper.Get().Logger.Log($"Failed to collect {SampleType}: " + ex.Message, LoggingLevel.Normal, GetType());
						}

						// let the system know that we polled but didn't get any data
						if (data.Count == 0)
						{
							data.Add(null);
						}

						completionSource.SetResult(data);
					})));

				return completionSource.Task;
			}
		}

		public abstract class CharacteristicCollector : HealthDataCollector
		{
			private HKCharacteristicTypeIdentifier _characteristicType;

			public HKCharacteristicTypeIdentifier CharacteristicType
			{
				get
				{
					return _characteristicType;
				}
				set
				{
					_characteristicType = value;

					ObjectType = HKCharacteristicType.Create(value);
					Key = value.ToString();
				}
			}

			public string LastValue { get; set; }

			protected List<HealthDatum> CreateData(string currentValue)
			{
				List<HealthDatum> data = new List<HealthDatum>();

				if (currentValue != LastValue)
				{
					HealthDatum datum = new HealthDatum(CharacteristicType.ToString(), currentValue, null, null, DateTime.Now, DateTime.Now, DateTimeOffset.UtcNow);

					LastValue = currentValue;

					data.Add(datum);
				}
				else
				{
					data.Add(null);
				}

				return data;
			}
			protected bool CanCreateData(NSError error)
			{
				if (error != null)
				{
					SensusServiceHelper.Get().Logger.Log($"Failed to collect {CharacteristicType}: " + error.Description, LoggingLevel.Normal, GetType());

					return false;
				}

				return true;
			}

			public CharacteristicCollector()
			{

			}

			public CharacteristicCollector(HKHealthStore healthStore, HKCharacteristicTypeIdentifier characteristicType) : base(healthStore)
			{
				CharacteristicType = characteristicType;
			}
		}

		public class BiologicalSexCollector : CharacteristicCollector
		{
			public BiologicalSexCollector()
			{

			}

			public BiologicalSexCollector(HKHealthStore healthStore) : base(healthStore, HKCharacteristicTypeIdentifier.BiologicalSex)
			{

			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				HKBiologicalSexObject biologicalSex = HealthStore.GetBiologicalSex(out NSError error);

				if (CanCreateData(error))
				{
					string currentValue = biologicalSex.BiologicalSex.ToString();

					return Task.FromResult(CreateData(currentValue));
				}

				return Task.FromResult(new List<HealthDatum> { null });
			}
		}

		public class BirthdateCollector : CharacteristicCollector
		{
			public BirthdateCollector()
			{

			}

			public BirthdateCollector(HKHealthStore healthStore) : base(healthStore, HKCharacteristicTypeIdentifier.DateOfBirth)
			{

			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				NSDateComponents birthdate = HealthStore.GetDateOfBirthComponents(out NSError error);

				if (birthdate != null && CanCreateData(error))
				{
					string currentValue = NSIso8601DateFormatter.Format(birthdate.Date, birthdate.TimeZone, NSIso8601DateFormatOptions.FullDate);

					return Task.FromResult(CreateData(currentValue));
				}

				return Task.FromResult(new List<HealthDatum> { null });
			}
		}

		public class BloodTypeCollector : CharacteristicCollector
		{
			public BloodTypeCollector()
			{

			}

			public BloodTypeCollector(HKHealthStore healthStore) : base(healthStore, HKCharacteristicTypeIdentifier.BloodType)
			{

			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				HKBloodTypeObject bloodType = HealthStore.GetBloodType(out NSError error);

				if (CanCreateData(error))
				{
					string currentValue = bloodType.BloodType.ToString();

					return Task.FromResult(CreateData(currentValue));
				}

				return Task.FromResult(new List<HealthDatum> { null });
			}
		}

		public class FitzpatrickSkinTypeCollector : CharacteristicCollector
		{
			public FitzpatrickSkinTypeCollector()
			{

			}

			public FitzpatrickSkinTypeCollector(HKHealthStore healthStore) : base(healthStore, HKCharacteristicTypeIdentifier.FitzpatrickSkinType)
			{

			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				HKFitzpatrickSkinTypeObject skinType = HealthStore.GetFitzpatrickSkinType(out NSError error);

				if (CanCreateData(error))
				{
					string currentValue = skinType.SkinType.ToString();

					return Task.FromResult(CreateData(currentValue));
				}

				return Task.FromResult(new List<HealthDatum> { null });
			}
		}

		public class WheelchairUseCollector : CharacteristicCollector
		{
			public WheelchairUseCollector()
			{

			}

			public WheelchairUseCollector(HKHealthStore healthStore) : base(healthStore, HKCharacteristicTypeIdentifier.WheelchairUse)
			{

			}

			public override Task<List<HealthDatum>> GetDataAsync()
			{
				HKWheelchairUseObject wheelchairUse = HealthStore.GetWheelchairUse(out NSError error);

				if (CanCreateData(error))
				{
					string currentValue = wheelchairUse.WheelchairUse.ToString();

					return Task.FromResult(CreateData(currentValue));
				}

				return Task.FromResult(new List<HealthDatum> { null });
			}
		}
		#endregion

		private Dictionary<string, HealthDataCollector> _collectors;
		public List<HealthDataCollector> Collectors { get; set; }

		[OnOffUiProperty("Collect Biological Sex:", true, 60)]
		public bool CollectBiologicalSex { get; set; }
		[OnOffUiProperty("Collect Birthdate:", true, 61)]
		public bool CollectBirthdate { get; set; }
		[OnOffUiProperty("Collect Blood Type:", true, 62)]
		public bool CollectBloodType { get; set; }
		[OnOffUiProperty("Collect Body Mass Index:", true, 63)]
		public bool CollectBodyMassIndex { get; set; }
		[OnOffUiProperty("Collect Distance Walking/Ranning:", true, 64)]
		public bool CollectDistanceWalkingRunning { get; set; }
		[OnOffUiProperty("Collect Fitzpatrick Skin Type:", true, 65)]
		public bool CollectFitzpatrickSkinType { get; set; }
		[OnOffUiProperty("Collect Flights Climbed:", true, 66)]
		public bool CollectFlightsClimbed { get; set; }
		[OnOffUiProperty("Collect Heart Rate:", true, 67)]
		public bool CollectHeartRate { get; set; }
		[OnOffUiProperty("Collect Height:", true, 68)]
		public bool CollectHeight { get; set; }
		[OnOffUiProperty("Collect Number Of Times Fallen:", true, 69)]
		public bool CollectNumberOfTimesFallen { get; set; }
		[OnOffUiProperty("Collect Step Count:", true, 70)]
		public bool CollectStepCount { get; set; }
		[OnOffUiProperty("Collect Weight:", true, 71)]
		public bool CollectWeight { get; set; }
		[OnOffUiProperty("Collect Wheelchair Use:", true, 72)]
		public bool CollectWheelchairUse { get; set; }

		private void InitializeCollector(bool enabled, HealthDataCollector collector)
		{
			if (_collectors.TryGetValue(collector.Key, out HealthDataCollector existing))
			{
				if (enabled == false)
				{
					_collectors.Remove(collector.Key);
				}
				else
				{
					existing.HealthStore = _healthStore;
				}
			}
			else if (enabled)
			{
				_collectors[collector.Key] = collector;
			}
		}

		private void InitializeCollectors()
		{
			//if (Collectors == null)
			//{
			//	Collectors = new List<HealthDataCollector>();
			//}

			_collectors = Collectors.ToDictionary(x => x.Key);

			InitializeCollector(CollectBiologicalSex, new BiologicalSexCollector(_healthStore));
			InitializeCollector(CollectBirthdate, new BirthdateCollector(_healthStore));
			InitializeCollector(CollectBloodType, new BloodTypeCollector(_healthStore));
			InitializeCollector(CollectBodyMassIndex, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.BodyMassIndex, HKUnit.Count));
			InitializeCollector(CollectDistanceWalkingRunning, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.DistanceWalkingRunning, HKUnit.Mile));
			InitializeCollector(CollectFitzpatrickSkinType, new FitzpatrickSkinTypeCollector(_healthStore));
			InitializeCollector(CollectFlightsClimbed, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.FlightsClimbed, HKUnit.Count));
			InitializeCollector(CollectHeartRate, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.HeartRate, HKUnit.Count));
			InitializeCollector(CollectHeight, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.Height, HKUnit.Inch));
			InitializeCollector(CollectNumberOfTimesFallen, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.NumberOfTimesFallen, HKUnit.Count));
			InitializeCollector(CollectStepCount, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.StepCount, HKUnit.Count));
			InitializeCollector(CollectWeight, new SampleCollector(_healthStore, HKQuantityTypeIdentifier.BodyMass, HKUnit.Pound));
			InitializeCollector(CollectWheelchairUse, new WheelchairUseCollector(_healthStore));

			Collectors = _collectors.Values.ToList();
		}

		protected override async Task InitializeAsync()
		{
			await base.InitializeAsync();

			if (HKHealthStore.IsHealthDataAvailable)
			{
				InitializeCollectors();

				HKObjectType[] objectTypes = Collectors.Select(x => x.ObjectType).ToArray();

				if (objectTypes.Any())
				{
					NSSet objectTypesToRead = NSSet.MakeNSObjectSet(objectTypes);

					(bool success, NSError error) = await _healthStore.RequestAuthorizationToShareAsync(new NSSet(), objectTypesToRead);
					
					if (success == false)
					{
						string message = "Failed to request HealthKit authorization: " + (error?.ToString() ?? "[no details]");

						SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());

						throw new Exception(message);
					}
				}
				else
				{
					string message = "The Health probe requres at least one data type to be collected.";

					SensusServiceHelper.Get().Logger.Log(message, LoggingLevel.Normal, GetType());

					throw new Exception(message);
				}
			}
		}

		protected override async Task<List<Datum>> PollAsync(CancellationToken cancellationToken)
		{
			List<Datum> data = new List<Datum>();

			foreach (HealthDataCollector collector in Collectors)
			{
				data.AddRange(await collector.GetDataAsync());
			}

			return data;
		}
	}
}
