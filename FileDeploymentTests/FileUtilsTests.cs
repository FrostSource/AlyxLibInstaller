namespace FileDeployment.Tests;

[TestClass()]
public class FileUtilsTests
{
    private const string TestFile01Hash = "16c83711817810b1de5517e0dd0d1f584e51353c336f847510cc5dbfe32154c3";

    [TestMethod()]
    [DeploymentItem(@"TestData\FileHash01.txt")]
    public void GetFileHashTest()
    {
        string testFile = @"FileHash01.txt";

        // Test file must exist
        Assert.IsTrue(File.Exists(testFile));
        var fileHash = FileUtils.GetFileHash(testFile);
        Assert.IsTrue(fileHash == TestFile01Hash);

        var lines = File.ReadAllLines(testFile);
        var content = string.Join(Environment.NewLine, lines);
        var rawContent = File.ReadAllText(testFile);

        Assert.IsTrue(fileHash == FileUtils.GetStringHash(content));
        Assert.IsTrue(fileHash == FileUtils.GetStringHash(rawContent));

        Directory.CreateDirectory("DummyFolder");
        Assert.ThrowsException<ArgumentException>(() => FileUtils.GetFileHash("DummyFolder"));
    }

    private const string TestString = "This is a test string for GetStringHashTest()";

    [TestMethod()]
    public void GetStringHashTest()
    {
        var hash = FileUtils.GetStringHash(TestString);

        // Check if the hash is a string
        Assert.IsInstanceOfType(hash, typeof(string));

        // Check if the hash is not null or empty
        Assert.IsFalse(string.IsNullOrEmpty(hash));

        // Check if the hash is consistent for the same input
        var hash2 = FileUtils.GetStringHash(TestString);
        Assert.AreEqual(hash, hash2);

        // Check if different strings produce different hashes
        var differentString = "This is a different test string";
        var differentHash = FileUtils.GetStringHash(differentString);
        Assert.AreNotEqual(hash, differentHash);

        // Check if empty string produces a hash
        var emptyStringHash = FileUtils.GetStringHash(string.Empty);
        Assert.IsFalse(string.IsNullOrEmpty(emptyStringHash));

        // Check if null string throws an exception
        Assert.ThrowsException<ArgumentNullException>(() => FileUtils.GetStringHash(null!));
    }
}