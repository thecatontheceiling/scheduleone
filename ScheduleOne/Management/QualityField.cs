using ScheduleOne.ItemFramework;
using ScheduleOne.Persistence.Datas;
using UnityEngine.Events;

namespace ScheduleOne.Management;

public class QualityField : ConfigField
{
	public UnityEvent<EQuality> onValueChanged = new UnityEvent<EQuality>();

	public EQuality Value { get; protected set; } = EQuality.Standard;

	public QualityField(EntityConfiguration parentConfig)
		: base(parentConfig)
	{
	}

	public void SetValue(EQuality value, bool network)
	{
		Value = value;
		if (network)
		{
			base.ParentConfig.ReplicateField(this);
		}
		if (onValueChanged != null)
		{
			onValueChanged.Invoke(Value);
		}
	}

	public override bool IsValueDefault()
	{
		return Value == EQuality.Standard;
	}

	public QualityFieldData GetData()
	{
		return new QualityFieldData(Value);
	}

	public void Load(QualityFieldData data)
	{
		if (data != null)
		{
			SetValue(data.Value, network: true);
		}
	}
}
