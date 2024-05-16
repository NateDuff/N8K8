/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.{razor,html,cshtml}"
    ],
    theme: {
        extend: {
            colors: {
                osGreen: {
                    400: '#00985B',
                },
                osLightGreen: {
                    400: '#ABFBC6'
                },
                osLightGray: {
                    400: '#D4D4D4'
                },
                osGray: {
                    400: '#929292'
                },
                osDarkGray: {
                    400: '#373737'
                },
                osLightBlue: {
                    400: '#B6BDFF'
                },
                osBlue: {
                    400: '#5564FF'
                },
            }
        }
    },
  plugins: [],
}
