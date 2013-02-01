using System;
using System.Runtime.InteropServices;

namespace ABB.SrcML.VisualStudio.SrcMLService
{
	/// <summary>
	/// This is the interface implemented by the local service.
	/// Notice that we have to define this interface as COM visible so that 
	/// it will be possible to query for it from the native version of IServiceProvider.
	/// </summary>
	[Guid("04079195-ce4d-4683-aec3-e2f2be23b935")]
	[ComVisible(true)]
	public interface ISrcMLLocalService
	{
		int LocalServiceFunction();
	}

	/// <summary>
	/// This interface is used to define the Type or Guid that identifies the service.
	/// It is not strictly required because our service will implement only one interface,
	/// but in case of services that implement multiple interfaces it is good practice to define
	/// a different type to identify the service itself.
	/// </summary>
	[Guid("ed840427-1df8-4d3a-85eb-38847fba93f5")]
    public interface SSrcMLLocalService
	{
	}
}
