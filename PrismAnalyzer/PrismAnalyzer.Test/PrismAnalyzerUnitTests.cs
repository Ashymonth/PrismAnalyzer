using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Prism.Mvvm;
using VerifyCS = PrismAnalyzer.Test.CSharpCodeFixVerifier<
    PrismAnalyzer.PrismAnalyzerAnalyzer,
    PrismAnalyzer.PrismAnalyzerCodeFixProvider>;

namespace PrismAnalyzer.Test
{
    [TestClass]
    public class PrismAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"using System;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;
    namespace ConsoleApp2
{
    public class AEntity
    {
        public int Id { get; set; }
    }
public class {|#0:TypeName|}{}
    public class Model<TEntity> where TEntity : AEntity
    {
        public Model(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; }

    }
    public class ImageEntity : AEntity
{
    public string Name {get; set;}
}
    public class ImageModel : AModel<ImageEntity> 
{
       public ImageModel(ImageEntity entity)
        {
            Entity = entity;
        }
public string Name {get; set;}
}
}
";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("PrismAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }

    public class AEntity
    {
        public int Id { get; set; }
    }
    public class AModel<TEntity> : BindableBase where TEntity : AEntity
    {
        protected AModel(TEntity entity)
        {
            Entity = entity;
        }

        public TEntity Entity { get; }

    }
    public class ImageEntity : AEntity
    {
        public string Name { get; set; }
    }

    public class ImageModel : AModel<ImageEntity>
    {
        public ImageModel(ImageEntity entity) : base(entity)
        {

        }

        public string Name { get => Entity.Name; set { Entity.Name = value; RaisePropertyChanged(); } }

        public ICollection<string> Ty { get; set; }
    }
}

