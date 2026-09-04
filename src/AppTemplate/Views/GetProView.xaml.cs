using AppTemplate.Core.Navigation;
using AppTemplate.ViewModels;

namespace AppTemplate.Views;

[NavigationInfo(NavigationSection.GetPro)]
public partial class GetProViewBase : ViewBase<GetProViewModel> { }

public sealed partial class GetProView : GetProViewBase
{
    public GetProView()
    {
        this.InitializeComponent();
    }
}
