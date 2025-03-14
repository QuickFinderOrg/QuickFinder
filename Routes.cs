namespace group_finder;

public static class StudentRoutes
{
    public static string Home() => "/Index";
    public static string Login() => "/Login";
    public static string Groups() => "/Student/Groups";
    public static string Matching() => "/Student/Matching";
    public static string Preferences() => "/Student/Preferences";
}

public static class TeacherRoutes
{
    public static string CourseGroups() => "/Teacher/CourseOverview";
    public static string EditGroup() => "/Teacher/EditGroup";
    public static string CreateCourse() => "/Teacher/CreateCourse";
}

public static class AdminRoutes
{
    public static string AddStudent() => "/Admin/AddStudents";
    public static string Students() => "/Admin/Students";
    public static string UserOverview() => "/Admin/UserOverview";
}