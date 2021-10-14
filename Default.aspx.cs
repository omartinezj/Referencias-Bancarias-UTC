using conekta;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Referencias_Bancarias_UTC
{
    public partial class Default : System.Web.UI.Page
    {
        public string referencia_oxxo = null;
        public string codigo_barras = null;
        protected void Page_Load(object sender, EventArgs e)
        {
            Consulta_Datos();

            ///PRUEBAS///
            ///Prueba 1 UTC ///
            //crear_referencia("MIGUEL PEDRAZA PACHECO", "8009531305", "soporte@lottuseducation.com", "200040218202042074", "2");
            //pdf_referencia("MIGUEL PEDRAZA PACHECO", "OPCION COMBO II CERT BACH CETC", "340.00", "2", referencia_oxxo, codigo_barras);

            ///Prueba 2 CETC ///
            //crear_referencia("LUIS LAZCANO RIVERA", "8009531305", "soporte@lottuseducation.com", "180019037202143137", "1");
            //pdf_referencia("LUIS LAZCANO RIVERA", "COLEGIATURA", "1740.00", "1", referencia_oxxo, codigo_barras);

            ///Prueba 3 UTC ///
            //crear_referencia("LUIS TREJO VAZQUEZ", "8009531305", "soporte@lottuseducation.com", "200040419202146046", "2");
            //pdf_referencia("LUIS TREJO VAZQUEZ", "COLEGIATURA", "298.00", "2", referencia_oxxo, codigo_barras);
            ///Prueba 4 CETC///
            //crear_referencia("KARINA MARTINEZ MARTINEZ", "8009531305", "soporte@lottuseducation.com", "190023648202143108", "1");
            //pdf_referencia("KARINA MARTINEZ MARTINEZ", "COLEGIATURA", "1740.00", "1", referencia_oxxo, codigo_barras);


            ///PRODUCCION///
            ///Prueba 1 UTC ///
            //crear_referencia("MIGUEL PEDRAZA PACHECO", "8009531305", "soporte@lottuseducation.com", "200040218202042085", "2");
            //pdf_referencia("MIGUEL PEDRAZA PACHECO", "COLEGIATURA", "20.00", "2", referencia_oxxo, codigo_barras);
            ///Prueba 2 CETC ///
            //crear_referencia("LUIS LAZCANO RIVERA", "8009531305", "soporte@lottuseducation.com", "180019037202143142", "1");
            //pdf_referencia("LUIS LAZCANO RIVERA", "COLEGIATURA", "21.00", "1", referencia_oxxo, codigo_barras);
            ///Prueba 3 UTC ///
            //crear_referencia("LUIS TREJO VAZQUEZ", "8009531305", "soporte@lottuseducation.com", "200040419202146068", "2");
            //pdf_referencia("LUIS TREJO VAZQUEZ", "COLEGIATURA", "22.00", "2", referencia_oxxo, codigo_barras);
            ///Prueba 4 CETC///
            //crear_referencia("KARINA MARTINEZ MARTINEZ", "8009531305", "soporte@lottuseducation.com", "190023648202143108", "1");
            //pdf_referencia("KARINA MARTINEZ MARTINEZ", "COLEGIATURA", "23.00", "1", referencia_oxxo, codigo_barras);
        }

        protected void Consulta_Datos()
        {

            string userId = Convert.ToString(Request.QueryString["matricula"]);
            string transa = Convert.ToString(Request.QueryString["numero"]);


            // Conexión hacia Banner
            string strQueryConcepto = @"select RTRIM(LTRIM(spriden_first_name||' '||spriden_mi))||' '||spriden_last_name nombre, tbbdetc_desc, tbraccd_balance, tbraccd_term_code, to_char(to_date(tbraccd_effective_date),'dd/MM/yyyy'),CASE WHEN SGBSTDN_LEVL_CODE='BA' AND SGBSTDN_CAMP_CODE='ZRB' THEN 2 ELSE 1 END Compania " +
                                        "from  tbraccd, tbbdetc,spriden " +
                                        "INNER JOIN SGBSTDN ON SGBSTDN_PIDM=SPRIDEN_PIDM " +
                                        "where spriden_id='" + userId + "' and spriden_change_ind is null " +
                                        "and   tbraccd_pidm=spriden_pidm and tbraccd_tran_number= " + transa + "  and tbbdetc_detail_code=tbraccd_detail_code " +
                                        "AND SGBSTDN_TERM_CODE_EFF =(SELECT MAX(X.SGBSTDN_TERM_CODE_EFF) FROM SGBSTDN X WHERE X.SGBSTDN_PIDM=SPRIDEN_PIDM ) ";

            OracleConnection oracleConnection =
             new OracleConnection(ConfigurationManager.ConnectionStrings["ConexionNOAH"].ConnectionString);
            OracleDataAdapter adapter = new OracleDataAdapter();
            DataSet ds1 = new DataSet();
            try
            {

                OracleCommand command = new OracleCommand(strQueryConcepto, oracleConnection);
                adapter.SelectCommand = command;
                adapter.Fill(ds1);
                adapter.Dispose();
                command.Dispose();
            }
            catch (Exception ex)
            {
                registro_log(1, "Consulta_Datos", ex.Message, ex.ToString(), strQueryConcepto);
            }
            finally
            {
                oracleConnection.Close();
            }


            string concepto = ds1.Tables[0].Rows[0][1].ToString();
            double importe = Convert.ToDouble(ds1.Tables[0].Rows[0][2].ToString());
            string nombre = ds1.Tables[0].Rows[0][0].ToString();
            string company = ds1.Tables[0].Rows[0][5].ToString();
            string periodo = ds1.Tables[0].Rows[0][3].ToString();
            string referencia = userId + periodo + transa.PadLeft(3, '0');

            try
            {
                crear_referencia(nombre, "8009531305", "soporte@lottuseducation.com", referencia, company);
            }
            catch (Exception ex)
            {
                registro_log(1, "Referencia_Oxxo", ex.Message, ex.ToString(), nombre);
            }

            try
            {
                pdf_referencia(nombre, concepto, importe.ToString("#,##.00"), company, referencia_oxxo, codigo_barras);
            }
            catch (Exception ex)
            {
                registro_log(1, "pdf_referencia", ex.Message, ex.ToString(), referencia_oxxo);
            }


        }
        private void pdf_referencia(string Nombre, string concepto, string importe, string company, string referencia_oxxo, string codigo_barras)
        {

            Document doc = new Document(PageSize.A4, 25f, 25f, 25f, 25f);
            //Document doc = new Document(PageSize.A4);

            string path = Server.MapPath("PDF");
            string strLogoPath = Server.MapPath("Images") + "//Codebar//logo-utc.jpg";  //
            string strLogoBancoAzteca = Server.MapPath("Images") + "//Codebar//bancoazteca-logo.png";
            string strLogoBancomer = Server.MapPath("Images") + "//Codebar//bancomer-logo.jpg";
            string strLogoSantander = Server.MapPath("Images") + "//Codebar//Santander-logo.jpg";
            string strLogoOxxo = Server.MapPath("Images") + "//Codebar//oxxopay_brand.png";
            string strbar = Server.MapPath("Images") + "//Codebar//pleca-utc1.jpg";
            string strbar2 = Server.MapPath("Images") + "//Codebar//pleca-utc3.jpg";

            string userId = Convert.ToString(Request.QueryString["matricula"]);
            string ref_bancoazteca = Convert.ToString(Request.QueryString["bancoazteca"]);
            string ref_bancomer = Convert.ToString(Request.QueryString["bancomer"]);
            string ref_santander = Convert.ToString(Request.QueryString["santander"]);

            PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(path + "\\" + ref_bancoazteca + ".pdf", FileMode.Create));
            doc.Open();
            doc.AddTitle("Referencia bancaria");
            doc.AddAuthor("Universidad Tres Culturas");

            //Fonts//
            var BundaySansFont = GetFontBunday(0, 0, 0, 12f);
            var BundaySansBoldFont = GetFontBundayB(4, 107, 97, 20f);
            var BundaySansBoldFont_titu = GetFontBundayB(0, 0, 0, 12f);
            var BundaySansSMBoldFont = GetFontBundayB(0, 0, 0, 12f);
            var BundaySansSMBoldFontMonto = GetFontBundayB(0, 0, 0, 15f);
            var BundaySansSMLigthFont = GetFontBundaySML(0, 0, 0, 14f);
            var BundaySansSMLigthFont10 = GetFontBundaySML(0, 0, 0, 10f);
            var BundaySansSMLigthFont9 = GetFontBundaySML(0, 0, 0, 9f);
            var BundaySansSMLigthFont_leyenda = GetFontBundaySML(0, 0, 0, 9f);
            var BundaySansSMLigthFont_leyenda_1 = GetFontBundaySMB(0, 0, 0, 9f);

            //Salto de Linea//
            Paragraph saltoDeLinea = new Paragraph("\n");

            //Pleca header//
            iTextSharp.text.Image bar = iTextSharp.text.Image.GetInstance(strbar);
            bar.ScaleAbsoluteWidth(doc.PageSize.Width);
            bar.SetAbsolutePosition(0, 830);

            //Logo ULA//
            iTextSharp.text.Image logo_ula = iTextSharp.text.Image.GetInstance(strLogoPath);
            logo_ula.SetAbsolutePosition(30, 760);

            //Fecha//
            PdfPTable table_fecha = new PdfPTable(1);
            table_fecha.WidthPercentage = 100;
            PdfPCell cell_fecha = new PdfPCell(new Paragraph("\n\nFecha: " + DateTime.Now.ToString("dd/MM/yyyy"), BundaySansFont));
            cell_fecha.HorizontalAlignment = Element.ALIGN_RIGHT;
            cell_fecha.Border = 0;
            table_fecha.AddCell(cell_fecha);

            //Encabezado//
            PdfPTable table_encabezado = new PdfPTable(2);
            table_encabezado.WidthPercentage = 90;
            PdfPCell cell_titulo = new PdfPCell(new Paragraph("FICHA DE DEPÓSITO ", BundaySansBoldFont));
            cell_titulo.Colspan = 2;
            cell_titulo.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_titulo.PaddingBottom = 10f;
            cell_titulo.Border = 0;
            table_encabezado.AddCell(cell_titulo);
            PdfPCell cell_nombre_lbl = new PdfPCell(new Paragraph("Nombre del Estudiante: ", BundaySansSMLigthFont));
            cell_nombre_lbl.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_nombre_lbl.PaddingLeft = 80f;
            cell_nombre_lbl.Border = 0;
            table_encabezado.AddCell(cell_nombre_lbl);
            PdfPCell cell_nombre = new PdfPCell(new Paragraph(Nombre, BundaySansSMBoldFont));
            cell_nombre.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_nombre.PaddingLeft = 15f;
            cell_nombre.Border = 0;
            table_encabezado.AddCell(cell_nombre);
            PdfPCell cell_matricula_lbl = new PdfPCell(new Paragraph("Matrícula: ", BundaySansSMLigthFont));
            cell_matricula_lbl.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_matricula_lbl.PaddingLeft = 80f;
            cell_matricula_lbl.Border = 0;
            table_encabezado.AddCell(cell_matricula_lbl);
            PdfPCell cell_matricula = new PdfPCell(new Paragraph(userId, BundaySansSMBoldFont));
            cell_matricula.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_matricula.PaddingLeft = 15f;
            cell_matricula.Border = 0;
            table_encabezado.AddCell(cell_matricula);
            PdfPCell cell_concepto_lbl = new PdfPCell(new Paragraph("Concepto cobro: ", BundaySansSMLigthFont));
            cell_concepto_lbl.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_concepto_lbl.PaddingLeft = 80f;
            cell_concepto_lbl.PaddingBottom = 10f;
            cell_concepto_lbl.Border = 0;
            table_encabezado.AddCell(cell_concepto_lbl);
            PdfPCell cell_concepto = new PdfPCell(new Paragraph(concepto, BundaySansSMBoldFont));
            cell_concepto.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_concepto.PaddingLeft = 15f;
            cell_concepto.Border = 0;
            cell_concepto.PaddingBottom = 10f;
            table_encabezado.AddCell(cell_concepto);
            //Linea 0//
            PdfContentByte cb = writer.DirectContent;
            cb.MoveTo(100, 645);
            cb.LineTo(doc.PageSize.Width - 100, 645);
            cb.SetLineWidth(0.5f);
            cb.Stroke();
            PdfPCell cell_monto_lbl = new PdfPCell(new Paragraph("MONTO A PAGAR: ", BundaySansSMLigthFont));
            cell_monto_lbl.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_monto_lbl.PaddingLeft = 80f;
            cell_monto_lbl.PaddingTop = 10f;
            cell_monto_lbl.Border = 0;
            table_encabezado.AddCell(cell_monto_lbl);
            PdfPCell cell_monto = new PdfPCell(new Paragraph("$ " + importe + " mxn", BundaySansSMBoldFontMonto));
            cell_monto.HorizontalAlignment = Element.ALIGN_LEFT;
            cell_monto.PaddingLeft = 15f;
            cell_monto.PaddingTop = 10f;
            cell_monto.Border = 0;
            table_encabezado.AddCell(cell_monto);




            //Tabla Referencias//
            PdfPTable table_referencia = new PdfPTable(3);
            table_referencia.WidthPercentage = 100;
            float[] width_table_referencias = new float[] { 35f, 40f, 25f };
            table_referencia.SetWidths(width_table_referencias);

            //Linea 1//
            PdfContentByte cb1 = writer.DirectContent;
            cb1.MoveTo(0, 610);
            cb1.LineTo(doc.PageSize.Width, 610);
            cb1.SetLineWidth(0.5f);
            cb1.Stroke();

            //Titulos//
            PdfPCell cell_titulo_banco = new PdfPCell(new Paragraph("Banco/Institución", BundaySansBoldFont_titu));
            cell_titulo_banco.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_titulo_banco.Border = 0;
            table_referencia.AddCell(cell_titulo_banco);
            PdfPCell cell_titulo_ref = new PdfPCell(new Paragraph("Referencia", BundaySansBoldFont_titu));
            cell_titulo_ref.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_titulo_ref.Border = 0;
            table_referencia.AddCell(cell_titulo_ref);
            PdfPCell cell_titulo_contrato = new PdfPCell(new Paragraph("Convenio/Cuenta CLABE", BundaySansBoldFont_titu));
            cell_titulo_contrato.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_titulo_contrato.Border = 0;
            table_referencia.AddCell(cell_titulo_contrato);

            //Banco Azteca//
            //Logo//
            PdfPCell cell_logo_bancoazteca = new PdfPCell();
            iTextSharp.text.Image logo_bancoazteca = iTextSharp.text.Image.GetInstance(strLogoBancoAzteca);
            logo_bancoazteca.Alignment = Element.ALIGN_CENTER;
            logo_bancoazteca.ScalePercent(10);
            cell_logo_bancoazteca.PaddingTop = 20f;
            cell_logo_bancoazteca.Border = 0;
            cell_logo_bancoazteca.AddElement(logo_bancoazteca);
            table_referencia.AddCell(cell_logo_bancoazteca);

            //Referencia//
            PdfPCell cell_ref_bancoazteca = new PdfPCell();
            cell_ref_bancoazteca.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_ref_bancoazteca.PaddingTop = 20f;
            cell_ref_bancoazteca.Border = 0;

            //Barcode Banco Azteca//
            iTextSharp.text.pdf.Barcode128 codebar_azteca = new Barcode128();
            codebar_azteca.TextAlignment = Element.ALIGN_CENTER;
            codebar_azteca.Code = ref_bancoazteca;
            codebar_azteca.StartStopText = false;
            codebar_azteca.CodeType = iTextSharp.text.pdf.Barcode128.CODE128;
            codebar_azteca.Extended = true;

            iTextSharp.text.Image barcode_azteca = codebar_azteca.CreateImageWithBarcode(cb, iTextSharp.text.BaseColor.BLACK, iTextSharp.text.BaseColor.BLACK);
            barcode_azteca.Alignment = Element.ALIGN_CENTER;
            barcode_azteca.ScaleAbsoluteWidth(150f);
            cell_ref_bancoazteca.AddElement(barcode_azteca);
            table_referencia.AddCell(cell_ref_bancoazteca);


            //convenio//
            string emisora = "";
            if (company == "1")
            {
                emisora = "Emisora:UTC";
            }
            else
            {
                emisora = "Emisora: Bachillerato-Universidad Tres Culturas";
            }
            PdfPCell cell_convenio_bancoazteca = new PdfPCell();

            //iTextSharp.text.Image referencia_bancoaztecaqr = iTextSharp.text.Image.GetInstance(barcodeQR(ref_bancoazteca), System.Drawing.Imaging.ImageFormat.Jpeg);
            //referencia_bancoaztecaqr.Alignment = Element.ALIGN_CENTER;
            //referencia_bancoaztecaqr.ScaleAbsolute(60, 60);
            cell_convenio_bancoazteca.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_convenio_bancoazteca.Border = 0;
            cell_convenio_bancoazteca.PaddingTop = 30f;
            //cell_convenio_bancoazteca.AddElement(referencia_bancoaztecaqr);
            cell_convenio_bancoazteca.AddElement(new Paragraph(emisora, BundaySansSMLigthFont10));
            table_referencia.AddCell(cell_convenio_bancoazteca);

            //Linea 2//
            PdfContentByte cb2 = writer.DirectContent;
            cb2.MoveTo(0, 490);
            cb2.LineTo(doc.PageSize.Width, 490);
            cb2.SetLineWidth(0.5f);
            cb2.SetLineDash(2.5f, 2.5f, 0f);
            cb2.Stroke();

            //Bancomer//
            //Logo//
            PdfPCell cell_logo_bancomer = new PdfPCell();
            iTextSharp.text.Image logo_bancomer = iTextSharp.text.Image.GetInstance(strLogoBancomer);
            logo_bancomer.Alignment = Element.ALIGN_CENTER;
            logo_bancomer.ScalePercent(15);
            cell_logo_bancomer.PaddingTop = 20f;
            cell_logo_bancomer.Border = 0;
            cell_logo_bancomer.AddElement(logo_bancomer);
            table_referencia.AddCell(cell_logo_bancomer);

            //Referencia//
            PdfPCell cell_ref_bancomer = new PdfPCell();
            cell_ref_bancomer.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_ref_bancomer.PaddingTop = 30f;
            cell_ref_bancomer.Border = 0;

            //Barcode Bancomer//
            iTextSharp.text.pdf.Barcode128 codebar_bbva = new Barcode128();
            codebar_bbva.TextAlignment = Element.ALIGN_CENTER;
            codebar_bbva.Code = ref_bancomer;
            codebar_bbva.StartStopText = false;
            codebar_bbva.CodeType = iTextSharp.text.pdf.Barcode128.CODE128;
            codebar_bbva.Extended = true;

            iTextSharp.text.Image barcode_bbva = codebar_bbva.CreateImageWithBarcode(cb, iTextSharp.text.BaseColor.BLACK, iTextSharp.text.BaseColor.BLACK);
            barcode_bbva.Alignment = Element.ALIGN_CENTER;
            barcode_bbva.ScaleAbsoluteWidth(150f);
            cell_ref_bancomer.AddElement(barcode_bbva);
            table_referencia.AddCell(cell_ref_bancomer);

            //convenio//
            string convenio = "";
            string leyenda_clabe = "*Si pagas por medio de SPEI deberás incluir en el campo de referencia o concepto el número de referencia que se presenta en tu ficha de pago, de lo contrario la transferencia será rechazada.";
            if (company == "1")
            {
                convenio = "Convenio CIE:1761528\n\nClabe:012914002017615285*";
            }
            else
            {
                convenio = "Convenio CIE:1761536\n\nClabe:012914002017615366*";
            }

            PdfPCell cell_convenio_bancomer = new PdfPCell(new Paragraph(convenio, BundaySansSMLigthFont10));
            cell_convenio_bancomer.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_convenio_bancomer.Border = 0;
            cell_convenio_bancomer.PaddingTop = 40f;
            table_referencia.AddCell(cell_convenio_bancomer);


            PdfPCell cell_leyenda_clabe = new PdfPCell(new Paragraph(leyenda_clabe, BundaySansSMLigthFont10));
            cell_leyenda_clabe.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_leyenda_clabe.Border = 0;
            cell_leyenda_clabe.Colspan = 3;
            table_referencia.AddCell(cell_leyenda_clabe);

            ///Modificación para agregar CLABE BBVA///
            //string clabe = "";
            //if (company == "1")
            //{
            //    clabe = "012914002017615285";
            //}
            //else
            //{
            //    clabe = "012914002017615366 ";
            //}
            //Chunk Part1 = new Chunk("Si tientes Banca Electrónica de otro banco, puedes realizar tu pago vía transferencia electrónica (SPEI) a la cuenta de BBVA: ", BundaySansSMLigthFont_leyenda);
            //Chunk Part2 = new Chunk(clabe, BundaySansSMLigthFont_leyenda_1);
            //Chunk Part3 = new Chunk(" en el campo de referencia o concepto de pago es indispensable que incluyas el número de referencia de tu cupón de pago.", BundaySansSMLigthFont_leyenda);
            //Phrase texto_clabe = new Phrase();
            //texto_clabe.Add(Part1);
            //texto_clabe.Add(Part2);
            //texto_clabe.Add(Part3);
            //PdfPCell cell_clabe_bancomer = new PdfPCell(texto_clabe);
            //cell_clabe_bancomer.HorizontalAlignment = Element.ALIGN_LEFT;
            //cell_clabe_bancomer.Border = 0;
            ////cell_clabe_bancomer.PaddingTop = 5f;
            //cell_clabe_bancomer.Colspan = 3;
            //table_referencia.AddCell(cell_clabe_bancomer);

            //Linea 3//
            PdfContentByte cb3 = writer.DirectContent;
            cb3.MoveTo(0, 390);
            cb3.LineTo(doc.PageSize.Width, 390);
            cb3.SetLineWidth(0.5f);
            cb3.SetLineDash(2.5f, 2.5f, 0f);
            cb3.Stroke();

            //Santander//
            //Logo//
            PdfPCell cell_logo_santander = new PdfPCell();
            iTextSharp.text.Image logo_santander = iTextSharp.text.Image.GetInstance(strLogoSantander);
            logo_santander.Alignment = Element.ALIGN_CENTER;
            cell_logo_santander.PaddingTop = 40f;
            cell_logo_santander.Border = 0;
            cell_logo_santander.AddElement(logo_santander);
            table_referencia.AddCell(cell_logo_santander);

            //Referencia//
            PdfPCell cell_ref_santander = new PdfPCell();
            cell_ref_santander.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_ref_santander.PaddingTop = 40f;
            cell_ref_santander.Border = 0;

            //Barcode Santander//
            iTextSharp.text.pdf.Barcode128 codebar_santander = new Barcode128();
            codebar_santander.TextAlignment = Element.ALIGN_CENTER;
            codebar_santander.Code = ref_santander;
            codebar_santander.StartStopText = false;
            codebar_santander.CodeType = iTextSharp.text.pdf.Barcode128.CODE128;
            codebar_santander.Extended = true;

            iTextSharp.text.Image barcode_santander = codebar_santander.CreateImageWithBarcode(cb, iTextSharp.text.BaseColor.BLACK, iTextSharp.text.BaseColor.BLACK);
            barcode_santander.Alignment = Element.ALIGN_CENTER;
            barcode_santander.ScaleAbsoluteWidth(150f);
            cell_ref_santander.AddElement(barcode_santander);
            table_referencia.AddCell(cell_ref_santander);
            //convenio//
            string convenio_santander = "";
            if (company == "1")
            {
                convenio_santander = "6914";
            }
            else
            {
                convenio_santander = "6908";
            }
            PdfPCell cell_convenio_santander = new PdfPCell(new Paragraph(convenio_santander, BundaySansSMLigthFont));
            cell_convenio_santander.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_convenio_santander.Border = 0;
            cell_convenio_santander.PaddingTop = 60f;
            table_referencia.AddCell(cell_convenio_santander);

            //Linea 4//
            //PdfContentByte cb4 = writer.DirectContent;
            //cb4.MoveTo(0, 300);
            //cb4.LineTo(doc.PageSize.Width, 300);
            //cb4.SetLineWidth(0.5f);
            //cb4.SetLineDash(2.5f, 2.5f, 0f);
            //cb4.Stroke();

            //Leyenda 1//
            string Leyenda_1 = "1. La referencia es válida durante 72 horas a partir de la fecha de impresión.\n" +
                              "2. La referencia es válida para pagar el concepto que seleccionaste.\n" +
                              "3. Para asegurar la captura correcta imprime y presenta en el banco o establecimiento.\n" +
                              "4. No pagues varios conceptos con una misma referencia.\n" +
                              "5. Genera una referencia por cada concepto que desees pagar.\n" +
                              "6. Una vez realizado tu pago, recibirás tu comprobante en un plazo máximo de 48 horas hábiles en el correo que registraste al ingresar a la UTC. Revisa tu bandeja spam\n";
            PdfPTable Leyenda_bancos = new PdfPTable(1);
            Leyenda_bancos.WidthPercentage = 100;
            PdfPCell cell_text = new PdfPCell(new Paragraph(Leyenda_1, BundaySansSMLigthFont_leyenda));
            cell_text.Border = 0;
            cell_text.SetLeading(0, 1f);
            cell_text.PaddingLeft = 20f;
            Leyenda_bancos.AddCell(cell_text);

            //Tabla Referencia OXXO//
            PdfPTable table_referencia_oxxo = new PdfPTable(3);
            table_referencia_oxxo.WidthPercentage = 100;
            float[] width_table_referencias_oxxo = new float[] { 35f, 40f, 25f };
            table_referencia_oxxo.SetWidths(width_table_referencias_oxxo);
            //Logo//
            PdfPCell cell_logo_oxxo = new PdfPCell();
            iTextSharp.text.Image logo_oxxo = iTextSharp.text.Image.GetInstance(strLogoOxxo);
            logo_oxxo.Alignment = Element.ALIGN_CENTER;
            logo_oxxo.ScalePercent(50);
            cell_logo_oxxo.PaddingTop = 30f;
            cell_logo_oxxo.Border = 0;
            cell_logo_oxxo.AddElement(logo_oxxo);
            table_referencia_oxxo.AddCell(cell_logo_oxxo);

            //Referencia//
            PdfPCell cell_ref_oxxo = new PdfPCell();
            iTextSharp.text.Image referencia_oxxo_img = iTextSharp.text.Image.GetInstance(new Uri(codigo_barras));
            //referencia_oxxo_img.ScaleAbsolute(100, 50);
            referencia_oxxo_img.ScaleAbsoluteWidth(150f);
            referencia_oxxo_img.Alignment = Element.ALIGN_CENTER;
            cell_ref_oxxo.HorizontalAlignment = Element.ALIGN_CENTER;
            cell_ref_oxxo.PaddingTop = 20f;
            cell_ref_oxxo.Border = 0;
            cell_ref_oxxo.AddElement(referencia_oxxo_img);
            cell_ref_oxxo.AddElement(new Paragraph(new Chunk("                                    " + referencia_oxxo, BundaySansSMLigthFont_leyenda))); ///
            table_referencia_oxxo.AddCell(cell_ref_oxxo);
            //convenio//
            PdfPCell cell_convenio_oxxo = new PdfPCell(new Paragraph(""));
            cell_convenio_oxxo.Border = 0;
            table_referencia_oxxo.AddCell(cell_convenio_oxxo);

            //Leyenda 2//
            string Leyenda_2 = "1. Acude a la tienda OXXO más cercana.\n" +
                                "2. Indica en caja que quieres realizar un pago de OXXOPay.\n" +
                                "3. Dicta al cajero el número de referencia en esta ficha.\n" +
                                "4. Solo te recibirán el pago con dinero en efectivo y el monto sin centavos.\n" +
                                "5. OXXO cobrará una comisión de $13.00 pesos al momento de realizar el pago.\n" +
                                "6. Al confirmar tu pago, el cajero te entregará un comprobante impreso. En él podrás verificar que se haya realizado correctamente.\n" +
                                "Conserva este comprobante de pago para futuras aclaraciones.\n" +
                                "7. La referencia sólo puede ser utilizada una vez.\n" +
                                "8. La referencia es válida durante diez días a partir de la fecha de impresión.\n";
            PdfPTable Leyenda_oxxo = new PdfPTable(1);
            Leyenda_oxxo.WidthPercentage = 100;
            PdfPCell cell_text_oxxo = new PdfPCell(new Paragraph(Leyenda_2, BundaySansSMLigthFont_leyenda));
            cell_text_oxxo.Border = 0;
            cell_text_oxxo.SetLeading(0, 1f);
            cell_text_oxxo.PaddingLeft = 20f;
            Leyenda_oxxo.AddCell(cell_text_oxxo);

            //Pleca footer//
            iTextSharp.text.Image bar_footer = iTextSharp.text.Image.GetInstance(strbar2);
            bar_footer.ScaleAbsoluteWidth(doc.PageSize.Width);
            bar_footer.SetAbsolutePosition(0, 0);






            doc.Add(bar);
            doc.Add(logo_ula);
            doc.Add(table_fecha);
            doc.Add(saltoDeLinea);
            doc.Add(saltoDeLinea);
            doc.Add(table_encabezado);
            doc.Add(saltoDeLinea);
            doc.Add(table_referencia);
            doc.Add(saltoDeLinea);
            doc.Add(Leyenda_bancos);
            doc.Add(table_referencia_oxxo);
            doc.Add(Leyenda_oxxo);
            doc.Add(bar_footer);
            doc.Close();

            byte[] bytes = File.ReadAllBytes(path + "/" + ref_bancoazteca + ".pdf");
            using (MemoryStream stream = new MemoryStream())
            {

                PdfReader reader = new PdfReader(bytes);
                using (PdfStamper stamper = new PdfStamper(reader, stream))
                {
                    Font fntTableFontHdr = FontFactory.GetFont("Times New Roman", 4, Font.BOLD,
                        BaseColor.BLACK);
                    Font fntTableFont = FontFactory.GetFont("Times New Roman", 8, Font.NORMAL,
                        BaseColor.BLACK);

                    int PageCount = reader.NumberOfPages;
                    int pages = reader.NumberOfPages;
                    for (int x = 1; x <= PageCount; x++)
                    {

                        //  ColumnText.ShowTextAligned(stamper.GetOverContent(x), Element.ALIGN_LEFT, new Phrase(String.Format("Hoja {0} de {1}", x, PageCount)), 500f, 15f, 0);

                    }
                }
                bytes = stream.ToArray();
                File.WriteAllBytes(path + "/" + ref_bancoazteca + ".pdf", bytes);
                string path1 = Server.MapPath("PDF") + "\\" + ref_bancoazteca + ".pdf";
                WebClient client = new WebClient();
                Byte[] buffer = client.DownloadData(path1);
                if (buffer != null)
                {

                    Response.Clear();
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("Content-Disposition", "attachment; filename=Referencia bancaria.pdf");
                    Response.ContentType = "application/pdf";
                    Response.Buffer = true;
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    Response.BinaryWrite(buffer);
                    Response.End();
                    Response.Close();

                }
            }

        }
        //protected static System.Drawing.Image barcodeQR(string reference)
        //{
        //    BarcodeWriter br = new BarcodeWriter
        //    {
        //        Format = ZXing.BarcodeFormat.QR_CODE,
        //        Options = new ZXing.Common.EncodingOptions
        //        {
        //            Height = 100,
        //            Width = 100,
        //            Margin = 0
        //        },
        //    };

        //    //BarcodeWriter br = new BarcodeWriter();
        //    //br.Format = BarcodeFormat.QR_CODE;
        //    System.Drawing.Bitmap bm = new System.Drawing.Bitmap(br.Write(reference));
        //    return bm;

        //}
        public static iTextSharp.text.Font GetFontBunday(int r, int g, int b, float size)
        {
            string fontName = "BundaySans";
            if (!FontFactory.IsRegistered(fontName))
            {
                var fontPath = HttpContext.Current.Server.MapPath("fonts") + "//BundaySans-Regular.otf";
                FontFactory.Register(fontPath, fontName);
            }

            var FontColour = new BaseColor(r, g, b); // optional... ints 0, 0, 0 are red, green, blue
            int FontStyle = iTextSharp.text.Font.NORMAL;  // optional
            float FontSize = size;  // optional

            return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, FontSize, FontStyle, FontColour);
            // last 3 arguments can be removed
        }
        public static iTextSharp.text.Font GetFontBundayB(int r, int g, int b, float size)
        {
            string fontName = "BundaySansB";
            if (!FontFactory.IsRegistered(fontName))
            {
                var fontPath = HttpContext.Current.Server.MapPath("fonts") + "//BundaySans-Bold.otf";
                FontFactory.Register(fontPath, fontName);
            }

            var FontColour = new BaseColor(r, g, b); // optional... ints 0, 0, 0 are red, green, blue
            int FontStyle = iTextSharp.text.Font.NORMAL;  // optional
            float FontSize = size;  // optional

            return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, FontSize, FontStyle, FontColour);
            // last 3 arguments can be removed
        }
        public static iTextSharp.text.Font GetFontBundaySMB(int r, int g, int b, float size)
        {
            string fontName = "BundaySansSMB";
            if (!FontFactory.IsRegistered(fontName))
            {
                var fontPath = HttpContext.Current.Server.MapPath("fonts") + "//BundaySans-SemiBold.otf";
                FontFactory.Register(fontPath, fontName);
            }

            var FontColour = new BaseColor(r, g, b); // optional... ints 0, 0, 0 are red, green, blue
            int FontStyle = iTextSharp.text.Font.NORMAL;  // optional
            float FontSize = size;  // optional

            return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, FontSize, FontStyle, FontColour);
            // last 3 arguments can be removed
        }
        public static iTextSharp.text.Font GetFontBundaySML(int r, int g, int b, float size)
        {
            string fontName = "BundaySansSML";
            if (!FontFactory.IsRegistered(fontName))
            {
                var fontPath = HttpContext.Current.Server.MapPath("fonts") + "//BundaySans-SemiLightUp.otf";
                FontFactory.Register(fontPath, fontName);
            }

            var FontColour = new BaseColor(r, g, b); // optional... ints 0, 0, 0 are red, green, blue
            int FontStyle = iTextSharp.text.Font.NORMAL;  // optional
            float FontSize = size;  // optional

            return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED, FontSize, FontStyle, FontColour);
            // last 3 arguments can be removed
        }
        protected void crear_referencia(string nombre, string telefono, string correo, string referencia, string compania)
        {
            if (ConfigurationManager.AppSettings["Ambiente"].ToString() == "1")
            {
                if (compania == "1") { Api.apiKey = "key_xLsxUqdci3e62PoPFidSMw"; } else { Api.apiKey = "key_J5opXLLEnBopK5kc8W6GFg"; }
            }
            else
            {
                if (compania == "1") { Api.apiKey = "key_1rQXCrkGrtYmy2pAit33sQ"; } else { Api.apiKey = "key_6wkWxCEULEQuHjmgCT4yiQ"; }
            }

            Api.version = "2.0.0";
            //string nombre = "Omar Martinez";
            //string telefono = "5555059804";
            //string correo = "omar.martinez@lottuseducation.com";
            //string referencia = "22005987600471";


            var fecha_exp = DateTime.Now.AddDays(10);
            var dateTime = new DateTime(fecha_exp.Year, fecha_exp.Month, fecha_exp.Day, fecha_exp.Hour, fecha_exp.Minute, fecha_exp.Second, DateTimeKind.Local);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (dateTime.ToUniversalTime() - epoch).TotalSeconds;
            string datos_cliente = @"{
                                        ""name"": """ + nombre + @""",
                                        ""phone"": """ + telefono + @""",
                                        ""email"": """ + correo + @""", 
                                        ""metadata"":{""description"" : ""Referencia UTC"", ""reference"" : """ + referencia + @"""},
                                        ""payment_sources"": []}";
            try
            {

                Customer customer = new conekta.Customer().create(datos_cliente);

                OfflineRecurrentReference reference = (OfflineRecurrentReference)customer.CreateOfflineRecurrentReference(@"{
                ""type"": ""oxxo_recurrent"",
                ""expires_at"": """ + unixDateTime + @"""}");
                referencia_oxxo = reference.reference;
                codigo_barras = reference.barcode_url;
            }
            catch (ConektaException ex)
            {
                foreach (JObject obj in ex.details)
                {
                    System.Console.WriteLine("\n [ERROR]:\n");
                    System.Console.WriteLine("message:\t" + obj.GetValue("message"));
                    System.Console.WriteLine("debug:\t" + obj.GetValue("debug_message"));
                    System.Console.WriteLine("code:\t" + obj.GetValue("code"));
                    registro_log(1, "Referencia_Oxxo", obj.GetValue("message").ToString(), obj.GetValue("debug_message").ToString(), "");
                }
            }


        }

        protected void registro_log(int debug_mode, string metodo, string var1, string var2, string var3)
        {
            if (debug_mode == 1)
            {
                StreamWriter sw = new StreamWriter(Server.MapPath("~/Logs/Error/") + "Error(" + metodo + ")_" + DateTime.Now.ToString("dd_MM_yyyy") + ".txt", true);
                sw.WriteLine("---------------------------------------" + DateTime.Now.ToString() + "--------------------------------------------");
                if (var1 != "") { sw.WriteLine(var1); }
                if (var2 != "") { sw.WriteLine(var2); }
                if (var3 != "") { sw.WriteLine(var3); }
                sw.WriteLine("------------------------------------------------------------------------------------------------------");
                sw.Close();
            }
            else if (debug_mode == 2)
            {
                StreamWriter sw = new StreamWriter(Server.MapPath("~/Logs/Debug/") + "Debug(" + metodo + ")_" + DateTime.Now.ToString("dd_MM_yyyy") + ".txt", true);
                sw.WriteLine("---------------------------------------" + DateTime.Now.ToString() + "--------------------------------------------");
                if (var1 != "") { sw.WriteLine(var1); }
                if (var2 != "") { sw.WriteLine(var2); }
                if (var3 != "") { sw.WriteLine(var3); }
                sw.WriteLine("------------------------------------------------------------------------------------------------------");
                sw.Close();
            }


        }
    }
}