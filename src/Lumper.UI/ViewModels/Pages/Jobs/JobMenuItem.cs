namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using Lumper.Lib.Jobs;

public interface IJobMenuItem<out T>; // Interface for covariance

public class JobMenuItem<T>(JobsViewModel jobsVm, Func<T> factory) : IJobMenuItem<T>
    where T : IJob
{
    public string Name => T.JobName;

    public void Create()
    {
        T job = factory();
        JobViewModel jobVm = JobsViewModel.CreateJobViewModel(job);
        jobsVm.Jobs.Add(jobVm);
    }
}
