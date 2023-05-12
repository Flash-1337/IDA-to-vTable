namespace IDA_to_vTable;


internal class Function
{
    public Function(string _class, string _name, string _args)
    {
        Class = _class;
        Name = _name;
        Args = _args;
        ReturnType = "virtual int";
    }

    public Function(string _class, string _name, string _args, string _return)
    {
        Class = _class;
        Name = _name;
        Args = _args;
        ReturnType = _return;
    }

    public string Name { get; set; }
    public string Class { get; set; }
    public string Args { get; set; }
    public string ReturnType { get; set; }

    public bool wasChanged { get; set; }
}