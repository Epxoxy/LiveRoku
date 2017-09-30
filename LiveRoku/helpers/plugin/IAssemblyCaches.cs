namespace LiveRoku {
    public interface IAssemblyCaches {
        bool tryGet (string fullName, out System.Reflection.Assembly assembly);
    }
}
