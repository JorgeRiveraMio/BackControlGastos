import unittest

from app.models.ocr_result import BoundingBox, OcrDocument, OcrLine
from app.parsers.yape_parser import YapeParser


def line(text: str, order: int, confidence: float = 0.95) -> OcrLine:
    return OcrLine(
        text=text,
        confidence=confidence,
        box=BoundingBox(left=10, top=20 + order * 30, right=200, bottom=40 + order * 30),
        reading_order=order,
    )


class YapeParserTests(unittest.TestCase):
    def test_ambiguous_amount_is_not_invented(self) -> None:
        document = OcrDocument(
            image_width=1080,
            image_height=1920,
            lines=[
                line("Yape", 0),
                line("5/50", 1, 0.41),
                line("Liz Mio", 2),
                line("16 ago. 2026 | 01:54 p. m.", 3),
                line("Destino", 4),
                line("Plin", 5),
                line("Nro. de operación", 6),
                line("4332887", 7),
            ],
        )

        result = YapeParser().parse(document)

        self.assertIsNone(result.amount)
        self.assertEqual("5/50", result.amount_raw)
        self.assertTrue(result.requires_review)
        self.assertEqual("Liz Mio", result.recipient)
        self.assertEqual("4332887", result.operation_number)

    def test_explicit_pen_amount_is_extracted(self) -> None:
        document = OcrDocument(
            image_width=1080,
            image_height=1920,
            lines=[line("Yape", 0), line("S/ 50.00", 1, 0.96)],
        )

        result = YapeParser().parse(document)

        self.assertEqual(50.0, result.amount)
        self.assertEqual("PEN", result.currency)
        self.assertFalse(result.requires_review)

    def test_currency_and_number_are_combined_only_when_visually_aligned(self) -> None:
        document = OcrDocument(
            image_width=1080,
            image_height=1920,
            lines=[
                line("Yape", 0),
                OcrLine(text="S/", confidence=0.99, box=BoundingBox(left=10, top=60, right=40, bottom=100), reading_order=1),
                OcrLine(text="50", confidence=0.98, box=BoundingBox(left=45, top=63, right=110, bottom=103), reading_order=2),
            ],
        )

        result = YapeParser().parse(document)

        self.assertEqual(50.0, result.amount)

    def test_transaction_date_is_extracted_from_noisy_ocr_line(self) -> None:
        document = OcrDocument(
            lines=[
                line("Yape", 0),
                line("S/50", 1, 0.99),
                line("Liz Mio", 2),
                line("白 16 ago. 2026 | ① 01:54 p. m.", 3),
            ],
        )

        result = YapeParser().parse(document)

        self.assertEqual("2026-08-16T13:54:00", result.date)
