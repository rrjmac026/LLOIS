namespace LLOIS.Services;

public class DashboardService(ApiClient api) : IDashboardService
{
    public async Task<ApiDashboard> GetAsync() =>
        await api.GetAsync<ApiDashboard>("api/dashboard")
            ?? throw new InvalidOperationException("Unexpected empty dashboard response.");
}