using Mammoth;

var converter = new DocumentConverter();
var result = converter.ConvertToHtml("document_1.docx");
var html = result.Value;
File.WriteAllText("output_1.html", html);

//using PDFiumZ;



//try
//{
//    using var document = new PdfDocument("document.pdf");

//    using var page = document.P.GetPage(0);

//    using var image = page.RenderToImage();

//    image.SaveAsSkiaPng("page-1.png");
//}
//finally
//{
//    PdfiumLibrary.Shutdown();
//}
