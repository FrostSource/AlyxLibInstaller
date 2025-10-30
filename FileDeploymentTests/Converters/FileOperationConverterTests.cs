using System.Text;
using System.Text.Json;

namespace FileDeployment.Converters.Tests;

[TestClass()]
public class FileOperationConverterTests
{
    private JsonSerializerOptions _options;

    private FileOperation? ReadJson(string json)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        var converter = new FileOperationConverter();

        // Read the first token to move the reader to the first token in the JSON
        if (!reader.Read()) // Ensure that Read is called to move to the first token
        {
            Assert.Fail("Failed to read the first token from JSON.");
        }

        var result = converter.Read(ref reader, typeof(FileOperation), _options);
        return result;
    }

    [TestInitialize]
    public void TestInitialize()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new FileOperationConverter() }
        };
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public void Read_InvalidJson_ShouldThrowJsonException()
    {
        // Invalid JSON: should throw exception because it's not an object
        string json = "[]";

        ReadJson(json);
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public void Read_MissingType_ShouldThrowJsonException()
    {
        // JSON is missing the 'Type' property
        string json = @"{ ""SomeOtherProperty"": ""value"" }";

        ReadJson(json);
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public void Read_NullType_ShouldThrowJsonException()
    {
        // JSON contains a 'Type' property but with a null value
        string json = @"{ ""Type"": null }";

        ReadJson(json);
    }

    [TestMethod]
    public void Read_ValidJson_ShouldDeserializeCorrectly()
    {
        // Dynamically test each valid alias from the FileOperationFactory
        foreach (var typeName in FileOperationFactory.Aliases)
        {
            string json;


            // Create a valid JSON string with the type
            if (typeof(IFileOperationWithDestination).IsAssignableFrom(FileOperationFactory.GetTypeFromName(typeName)))
            {
                // Assuming SomeOperationType is an operation requiring a destination
                json = $@"{{ ""Type"": ""{typeName}"", ""Source"": ""TestSource"", ""Destination"": ""TestDestination"" }}";
            }
            else
            {
                json = $@"{{ ""Type"": ""{typeName}"", ""Source"": ""TestSource"" }}";
            }

            var result = ReadJson(json);

            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(FileOperation));
            // Additional assertions to ensure correct deserialization based on type could be added here
        }
    }

    [TestMethod]
    public void Read_ValidDelete()
    {
        string json = @"{ ""Type"": ""Delete"", ""Source"": ""TestSource"" }";

        var result = ReadJson(json);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    [ExpectedException(typeof(JsonException))]
    public void Read_MissingDestination_ShouldThrowJsonException()
    {
        string json = @"{ ""Type"": ""Copy"", ""Source"": ""TestSource"" }"; // Missing 'Destination'

        ReadJson(json);
    }

    //[TestMethod]
    //public void Write_ValidObject_ShouldSerializeCorrectly()
    //{
    //    var operation = new SomeFileOperation // Replace with actual FileOperation subclass
    //    {
    //        Type = "SomeOperationType",
    //        SomeProperty = "value"
    //    };

    //    var writer = new Utf8JsonWriter(System.IO.MemoryStream.Null);
    //    var converter = new FileOperationConverter();

    //    converter.Write(writer, operation, _options);

    //    // Here we would need to test that the serialization output is correct.
    //    // This could involve capturing the output of the writer and verifying it.
    //}
}