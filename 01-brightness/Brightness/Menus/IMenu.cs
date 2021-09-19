namespace GraphFunc.Menus
{
    public interface IMenu
    {
        void Add(Form form);

        void Update(Form form);
        
        void Remove(Form form);

        string Name();
    }
}