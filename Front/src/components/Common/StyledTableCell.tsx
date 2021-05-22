import { TableCell } from '@material-ui/core';
import { withStyles, Theme, createStyles } from '@material-ui/core/styles';

export const StyledTableCell = withStyles((theme: Theme) =>
  createStyles({
    head: {
      backgroundColor: theme.palette.primary.dark,
      color: theme.palette.primary.contrastText,
    },
  }),
)(TableCell);