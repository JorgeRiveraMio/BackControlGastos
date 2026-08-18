import unittest
from datetime import date, datetime

from app.parsers.helpers.date_parser import parse_spanish_datetime


class SpanishDateParserTests(unittest.TestCase):
    def test_datetime_variants(self) -> None:
        cases = {
            "16 ago. 2026 | 01:54 p. m.": datetime(2026, 8, 16, 13, 54),
            "白 16 ago. 2026 | ① 01:54 p. m.": datetime(2026, 8, 16, 13, 54),
            "16 ago 2026 | 01:54 pm": datetime(2026, 8, 16, 13, 54),
            "16 agosto 2026 | 13:54": datetime(2026, 8, 16, 13, 54),
            "16 AGO. 2026 | 12:30 a. m.": datetime(2026, 8, 16, 0, 30),
            "16 ago. 2026 | 12:30 p. m.": datetime(2026, 8, 16, 12, 30),
        }
        for text, expected in cases.items():
            with self.subTest(text=text):
                self.assertEqual(expected, parse_spanish_datetime(text))

    def test_date_without_time_stays_a_date(self) -> None:
        self.assertEqual(date(2026, 8, 16), parse_spanish_datetime("16 ago. 2026"))

    def test_invalid_or_absent_dates_return_none(self) -> None:
        for text in ("texto sin fecha", "32 ago. 2026", "16 foo 2026", "16 ago. 2026 | 24:60"):
            with self.subTest(text=text):
                self.assertIsNone(parse_spanish_datetime(text))
