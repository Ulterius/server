using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;
using AgentInterface;
using AgentInterface.Api.Models;
using AgentInterface.Api.System;
using AgentInterface.Api.Win32;

namespace UlteriusAgent.Networking
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class InputAgent : IInputContract
    {

        public void HandleRightMouseDown()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                new InputSimulator().Mouse.RightButtonDown();
            }
        }

        public void HandleRightMouseUp()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                new InputSimulator().Mouse.RightButtonUp();
            }

        }

        public void MoveMouse(int x, int y)
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                var bounds = Display.GetWindowRectangle();
                x = checked((int)Math.Round(x * (65535 / (double)bounds.Width)));
                y = checked((int)Math.Round(y * (65535 / (double)bounds.Height)));
                new InputSimulator().Mouse.MoveMouseTo(x, y);
            }
        }

        public void MouseScroll(bool positive)
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                var direction = positive ? 10 : -10;
                new InputSimulator().Mouse.VerticalScroll(direction);
            }

        }


        public void HandleLeftMouseDown()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                new InputSimulator().Mouse.LeftButtonDown();
            }


        }

        public void HandleLeftMouseUp()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                new InputSimulator().Mouse.LeftButtonUp();
            }


        }

        public void HandleKeyDown(List<int> keyCodes)
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                foreach (var code in keyCodes)
                {
                    var virtualKey = (VirtualKeyCode)code;
                    new InputSimulator().Keyboard.KeyDown(virtualKey);

                }
            }

        }

        public void HandleKeyUp(List<int> keyCodes)
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                foreach (var code in keyCodes)
                {
                    var virtualKey = (VirtualKeyCode)code;
                    new InputSimulator().Keyboard.KeyUp(virtualKey);
                }
            }

        }

        public void HandleRightClick()
        {
            var inputDesktop = new Desktop();
            inputDesktop.OpenInput();
            var setCurrent = Desktop.SetCurrent(inputDesktop);
            if (setCurrent)
            {
                new InputSimulator().Mouse.RightButtonClick();
            }
        }

        [HandleProcessCorruptedStateExceptions]
        public float GetGpuTemp(string gpuName)
        {
            return SystemData.GetGpuTemp(gpuName);
        }

        public List<DisplayInformation> GetDisplayInformation()
        {
            return Display.DisplayInformation();
        }


        public List<float> GetCpuTemps()
        {
            return SystemData.GetCpuTemps();
        }
    }
}
