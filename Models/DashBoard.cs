namespace GelirGider.Models;

public class DashBoard
{
    public List<Categories> CategoriesList { get; set; }
    public List<Cags> CagsList { get; set; }
}

public class Categories
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int UserId { get; set; }
}

public class Cags
{
    public int Id { get; set; }
    public int Comes { get; set; }
    public int Goes { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
}