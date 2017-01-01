using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using AgentInterface.Api.Models;

namespace AgentInterface
{
    [ServiceContract(Namespace = "https://ulterius.io/")]
    public interface ITUlteriusContract
    {

        [OperationContract]
        byte[] GetCleanFrame();

        [OperationContract]
        byte[] GetFullFrame();

        [OperationContract]
        bool KeepAlive();

        [OperationContract(IsOneWay = true)]
        void HandleRightMouseDown();

        [OperationContract(IsOneWay = true)]
        void HandleRightMouseUp();

        [OperationContract(IsOneWay = true)]
        void MoveMouse(int x, int y);


        [OperationContract(IsOneWay = true)]
        void MouseScroll(bool positive);


        [OperationContract(IsOneWay = true)]
        void HandleLeftMouseDown();

        [OperationContract(IsOneWay = true)]
        void HandleLeftMouseUp();

        [OperationContract(IsOneWay = true)]
        void HandleKeyDown(List<int> keyCodes);

        [OperationContract(IsOneWay = true)]
        void HandleKeyUp(List<int> keyCodes);

        [OperationContract(IsOneWay = true)]
        void HandleRightClick();

        [OperationContract]
        float GetGpuTemp(string gpuName);

        [OperationContract]
        List<DisplayInformation> GetDisplayInformation();
    }
}
