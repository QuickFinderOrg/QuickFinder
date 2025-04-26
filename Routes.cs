namespace QuickFinder;

public static class StudentRoutes
{
    public static string Home() => "/Index";

    public static string Login() => "/Login";

    public static string Groups() => "/Student/Groups";

    public static string Matching() => "/Student/Matching";

    public static string Profile() => "/Account/Manage/Index";

    public static string Preferences() => "/Student/Preferences";

    public static string JoinGroup() => "/Student/JoinGroup";

    public static string CreateGroup() => "/Student/CreateGroup";

    public static string CourseOverview() => "/Student/CourseOverview";

    public static string CoursePreferences() => "/Student/CoursePreferences";
}

public static class TeacherRoutes
{
    public static string EditGroup() => "/Teacher/EditGroup";

    public static string CreateCourse() => "/Teacher/CreateCourse";

    public static string CourseOverview() => "/Teacher/CourseOverview";

    public static string AddServer() => "/Teacher/AddServer";

    public static string SplitGroup() => "/Teacher/SplitGroup";
}

public static class AdminRoutes
{
    public static string MatchmakingOverview() => "/Admin/MatchmakingOverview";

    public static string AddStudent() => "/Admin/AddStudents";

    public static string UserOverview() => "/Admin/UserOverview";

    public static string Servers() => "/Admin/Servers";
}
