import unittest

from app.models.ocr_result import OcrDocument, OcrLine
from app.parsers.document_classifier import DocumentClassifier, DocumentType


def document(*texts: str) -> OcrDocument:
    return OcrDocument(lines=[OcrLine(text=text, confidence=0.99, reading_order=index) for index, text in enumerate(texts)])


class DocumentClassifierTests(unittest.TestCase):
    def setUp(self) -> None:
        self.classifier = DocumentClassifier()

    def test_yape_structural_signals_win_over_plin_destination(self) -> None:
        result = self.classifier.detect(document(
            "yape", "Yapeaste!", "S/100", "Jorge Rivera", "DATOS DE LA TRANSACCIÓN",
            "Destino", "Plin", "Nro. de operación", "3839799",
        ))
        self.assertEqual(DocumentType.YAPE, result)

    def test_plin_structural_signals_win_over_yape_destination(self) -> None:
        result = self.classifier.detect(document(
            "Interbank", "plin", "Pago exitoso!", "s/25.00", "Enviado a:", "Liz M Mio D",
            "916 068 119 - Yape", "Comisión:", "Fecha y hora:", "25 Jul 2026 12:22 PM",
            "Código de operación:", "56530295",
        ))
        self.assertEqual(DocumentType.PLIN, result)

    def test_unrelated_receipt_is_generic(self) -> None:
        result = self.classifier.detect(document("SUPERMERCADO ABC", "RUC 20123456789", "TOTAL S/ 45.90"))
        self.assertEqual(DocumentType.GENERIC, result)
