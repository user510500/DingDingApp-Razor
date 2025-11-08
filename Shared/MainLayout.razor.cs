using Microsoft.AspNetCore.Components;
using DingDingApp.Services;

namespace DingDingApp.Shared
{
    public partial class MainLayout
    {
        [Inject] private ApiService apiService { get; set; } = default!;
        [Inject] private NavigationManager navigationManager { get; set; } = default!;
    }
}

