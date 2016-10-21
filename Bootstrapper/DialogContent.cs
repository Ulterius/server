using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Bootstrapper
{    
    public class DialogContent : Control
    {
        static DialogContent()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogContent), new FrameworkPropertyMetadata(typeof(DialogContent)));
        }

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(object), typeof(DialogContent), new PropertyMetadata(default(object)));

        public object Content
        {
            get { return (object) GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }
    }
}
