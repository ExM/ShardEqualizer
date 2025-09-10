using System.Runtime.Serialization;

namespace ShardEqualizer.Config;

[DataContract(Name = "Equalize")]
public class EqualizeConfig
{
    [DataMember(Name = "ShardEqualsPriority")]
    public double ShardEqualsPriority { get; set; }

    /// <summary>
    /// limit of relative deviation from the mean
    /// </summary>
    [DataMember(Name = "MaxRelativeDeviation")]
    public double MaxRelativeDeviation { get; set; }
}
