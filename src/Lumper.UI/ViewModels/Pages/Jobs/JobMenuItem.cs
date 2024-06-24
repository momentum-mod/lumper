namespace Lumper.UI.ViewModels.Pages.Jobs;

using System;
using Lib.Jobs;

public interface IJobMenuItem<out T>; // Interface for covariance

public class JobMenuItem<T>(JobsViewModel jobsVm) : IJobMenuItem<T> where T : IJob
{
    public string Name => T.JobName;

    public void Create()
    {
        if (Activator.CreateInstance(typeof(T)) is not IJob job)
            return;

        JobViewModel jobVm = JobsViewModel.CreateJobViewModel(job);
        jobsVm.Jobs.Add(jobVm);
    }
}
