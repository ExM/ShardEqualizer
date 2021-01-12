namespace MongoDB.ClusterMaintenance.Reporting
{
	public enum DataType
	{
		UnSharded,
		UnManaged,
		Fixed,
		Adjustable,
		
		/// <summary>
		/// All sharded data (sum of UnManaged, Fixed and Adjustable)
		/// </summary>
		Sharded,
		
		/// <summary>
		/// All balansed data (sum of UnSharded and Adjustable)
		/// </summary>
		Managed,
		
		/// <summary>
		/// All data
		/// </summary>
		Total,
	}
}