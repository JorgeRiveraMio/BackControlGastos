import unittest

from app.models.ocr_result import OcrDocument, OcrLine
from app.parsers.plin_parser import PlinParser


class PlinParserTests(unittest.TestCase):
    def test_extracts_interbank_plin_receipt(self) -> None:
        texts = [
            "Interbank", "plin", "Pago exitoso!", "s/25.00", "Enviado a:", "Liz M Mio D",
            "916 068 119 - Yape", "Comisión:", "GRATIS", "Fecha y hora:",
            "25 Jul 2026 12:22 PM", "Código de operación:", "56530295",
        ]
        document = OcrDocument(lines=[OcrLine(text=text, confidence=0.99, reading_order=index) for index, text in enumerate(texts)])

        result = PlinParser().parse(document)

        self.assertEqual("PLIN", result.document_type)
        self.assertEqual(25.0, result.amount)
        self.assertEqual("s/25.00", result.amount_raw)
        self.assertEqual("PEN", result.currency)
        self.assertEqual("Liz M Mio D", result.recipient)
        self.assertEqual("916068119", result.recipient_phone)
        self.assertEqual("Yape", result.destination)
        self.assertEqual("2026-07-25T12:22:00", result.date)
        self.assertEqual("56530295", result.operation_number)
        self.assertFalse(result.requires_review)

    def test_recipient_does_not_consume_next_known_label(self) -> None:
        document = OcrDocument(lines=[
            OcrLine(text="Enviado a:", confidence=0.99, reading_order=0),
            OcrLine(text="Fecha y hora:", confidence=0.99, reading_order=1),
        ])
        self.assertIsNone(PlinParser._recipient(document.lines))
