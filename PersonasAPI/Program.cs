using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);
//Utilizar inmemory database
builder.Services.AddDbContext<PersonaDb>(opt => opt.UseInMemoryDatabase("ListaPersonas"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Extra: implementación de swagger en la api.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//validación de campos correo electrónico, fecha de nacimiento
builder.Services.AddScoped<IValidator<Persona>, PersonaValidator>();
builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();

var app = builder.Build();

//mostrar un texto de referencia en caso de navegar por la raiz
app.MapGet("/", () => "API de Personas");

//Extra: implementación de swagger en la api.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//1.1.1       Listar personas en un json
app.MapGet("/listar_personas", async (PersonaDb db) =>
    await db.Personas.ToListAsync());

//obtener una persona especifica buscada por su codigo identificador
app.MapGet("/persona/{id}", async (int id, PersonaDb db) =>
    await db.Personas.FindAsync(id)
        is Persona persona
            ? Results.Ok(persona)
            : Results.NotFound());

//1.1.2       Registro de personas 
app.MapPost("/registrar_persona", async (IValidator<Persona> validator, IPersonaRepository repository, Persona persona, PersonaDb db) =>
{
    //validación de campos correo electrónico, fecha de nacimiento
    var validationResult = await validator.ValidateAsync(persona);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }
    if(db.Personas.Any(x => x.NroDocumento.ToUpper() == persona.NroDocumento.ToUpper())) 
    {
        return Results.Text("ERROR: no puede duplicar el número de documento");
    }

    //si los datos fueron validades exitosamente entonces se graba
    db.Personas.Add(persona);
    await db.SaveChangesAsync();
    
    return Results.Created($"/persona/{persona.Id}", persona);
});

//1.1.3       Editar persona.
app.MapPut("/editar_persona/{id}", async (IValidator<Persona> validator, IPersonaRepository repository, int id, Persona inputPersona, PersonaDb db) =>
{
    var persona = await db.Personas.FindAsync(id);

    if (persona is null) return Results.NotFound();

    //validación de campos correo electrónico, fecha de nacimiento,  duplicidad de personas por nro de documento
    var validationResult = await validator.ValidateAsync(inputPersona);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }
    
    if (db.Personas.Any(x => x.NroDocumento.ToUpper() == inputPersona.NroDocumento.ToUpper() && x.Id != persona.Id))
    {
        return Results.Text("ERROR: no puede duplicar el número de documento");
    }
    persona.NombreCompleto = inputPersona.NombreCompleto;
    persona.NroDocumento = inputPersona.NroDocumento;
    persona.CorreoElectronico = inputPersona.CorreoElectronico;
    persona.Telefono = inputPersona.Telefono;
    persona.FechaNacimiento = inputPersona.FechaNacimiento;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

//1.1.4    Eliminar persona
app.MapDelete("/eliminar_persona/{id}", async (int id, PersonaDb db) =>
{
    if (await db.Personas.FindAsync(id) is Persona persona)
    {
        db.Personas.Remove(persona);
        await db.SaveChangesAsync();
        return Results.Ok(persona);
    }

    return Results.NotFound();
});

app.Run();

class Persona
{
    public int Id { get; set; }
    public string? NombreCompleto { get; set; } //Nombre y Apellido
    public string? NroDocumento { get; set; } //Nro de Documento de identidad
    public string? CorreoElectronico { get; set; }
    public string? Telefono { get; set; }
    public string? FechaNacimiento { get; set; }//Fecha de nacimiento
}

class PersonaDb : DbContext
{
    public PersonaDb(DbContextOptions<PersonaDb> options)
        : base(options) { }

    public DbSet<Persona> Personas => Set<Persona>();
}

//validación de campos correo electrónico, fecha de nacimiento
class PersonaValidator : AbstractValidator<Persona>
{
    public PersonaValidator()
    {
        RuleFor(x => x.CorreoElectronico).NotEmpty().WithMessage("Debe completar el campo de correo electrónico");
        RuleFor(x => x.CorreoElectronico).EmailAddress().WithMessage("Formato de correo electrónico no válido");
        RuleFor(x => x.FechaNacimiento).NotEmpty().WithMessage("Debe completar el campo de fecha de nacimiento");
        RuleFor(x => Convert.ToDateTime(x.FechaNacimiento)).LessThan(x => DateTime.Now).WithMessage("Fecha de nacimiento debe ser menor a la fecha actual"); ;
        RuleFor(x => x.NroDocumento).NotEmpty().WithMessage("Debe completar el campo de número de documento");
    }
}
class PersonaRepository : IPersonaRepository
{
    public void AgregarPersona(Persona persona)
    {
    }
}
interface IPersonaRepository
{
    public void AgregarPersona(Persona persona);
}