using System.ServiceModel;
using AgentInterface.Api.Models;

namespace AgentInterface
{
    [ServiceContract(Namespace = "https://ulterius.io/")]
    public interface IFrameContract
    {

        [OperationContract]
        FrameInformation GetCleanFrame();

        [OperationContract]
        FrameInformation GetFullFrame();

        [OperationContract]
        bool KeepAlive();

    }
}