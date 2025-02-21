namespace group_finder;

public static class StudentRoutes
{
    public static string Home() => "/Index";
    public static string Groups() => "/Student/Groups";
    public static string Matching() => "/Student/Matching";
    public static string Preferences() => "/Student/Preferences";
}

public static class TeacherRoutes
{
    public static string CourseGroups() => "/Teacher/CourseGroups";
    public static string EditGroup() => "/Teacher/EditGroup";
}

public static class AdminRoutes
{
    public static string AddStudent() => "/AddStudents";
    public static string Students() => "/Students";
}