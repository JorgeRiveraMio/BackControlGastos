from __future__ import annotations

import re
from datetime import date, datetime

MONTHS = {
    "ene": 1, "enero": 1,
    "feb": 2, "febrero": 2,
    "mar": 3, "marzo": 3,
    "abr": 4, "abril": 4,
    "may": 5, "mayo": 5,
    "jun": 6, "junio": 6,
    "jul": 7, "julio": 7,
    "ago": 8, "agosto": 8,
    "sep": 9, "sept": 9, "septiembre": 9,
    "oct": 10, "octubre": 10,
    "nov": 11, "noviembre": 11,
    "dic": 12, "diciembre": 12,
    # Plin / Interbank receipts use English abbreviations.
    "jan": 1, "february": 2, "apr": 4, "april": 4,
    "june": 6, "july": 7, "aug": 8, "august": 8,
    "september": 9, "october": 10, "dec": 12, "december": 12,
}

MONTH_PATTERN = "|".join(sorted(MONTHS, key=len, reverse=True))
DATE_PATTERN = re.compile(
    rf"\b(?P<day>\d{{1,2}})\s+(?P<month>{MONTH_PATTERN})\.?\s+(?P<year>\d{{4}})\b",
    re.IGNORECASE,
)
TIME_PATTERN = re.compile(
    r"\b(?P<hour>\d{1,2}):(?P<minute>\d{2})(?:\s*(?P<meridiem>[ap])\s*\.?\s*m\.?)?",
    re.IGNORECASE,
)
TIME_LIKE_PATTERN = re.compile(r"\b\d{1,2}:\d{2}\b")


def parse_spanish_datetime(text: str) -> datetime | date | None:
    """Extract a Spanish or English month date and its optional time from OCR text.

    A date without a time stays a ``date`` so the API never fabricates midnight.
    """
    date_match = DATE_PATTERN.search(text)
    if date_match is None:
        return None

    try:
        parsed_date = date(
            year=int(date_match["year"]),
            month=MONTHS[date_match["month"].casefold()],
            day=int(date_match["day"]),
        )
    except ValueError:
        return None

    remainder = text[date_match.end():]
    time_match = TIME_PATTERN.search(remainder)
    if time_match is None:
        return None if TIME_LIKE_PATTERN.search(remainder) else parsed_date

    hour = int(time_match["hour"])
    minute = int(time_match["minute"])
    meridiem = time_match["meridiem"]
    if minute > 59 or hour > 23:
        return None
    if meridiem:
        if not 1 <= hour <= 12:
            return None
        if meridiem.casefold() == "a":
            hour = 0 if hour == 12 else hour
        else:
            hour = 12 if hour == 12 else hour + 12

    return datetime(parsed_date.year, parsed_date.month, parsed_date.day, hour, minute)


def has_spanish_date(text: str) -> bool:
    """Returns whether text contains a valid calendar date, regardless of its time."""
    date_match = DATE_PATTERN.search(text)
    if date_match is None:
        return False
    try:
        date(
            year=int(date_match["year"]),
            month=MONTHS[date_match["month"].casefold()],
            day=int(date_match["day"]),
        )
    except ValueError:
        return False
    return True
