namespace Controllers.Samples;

public interface IFakeService
{
    void Do();
}

public class FakeService : IFakeService
{
    public void Do()
    {
    }
}